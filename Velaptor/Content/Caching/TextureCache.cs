// <copyright file="TextureCache.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Content.Caching
{
    // ReSharper disable RedundantNameQualifier
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Abstractions;
    using System.Linq;
    using Velaptor.Content.Exceptions;
    using Velaptor.Content.Factories;
    using Velaptor.Graphics;
    using Velaptor.Guards;
    using Velaptor.Reactables.Core;
    using Velaptor.Reactables.ReactableData;
    using Velaptor.Services;

    // ReSharper restore RedundantNameQualifier

    /// <summary>
    /// Caches <see cref="ITexture"/> objects for retrieval at a later time.
    /// </summary>
    internal sealed class TextureCache : IItemCache<string, ITexture>
    {
        private const string DefaultTag = "[DEFAULT]";
        private const string TextureFileExtension = ".png";
        private const string FontFileExtension = ".ttf";
        private const string DefaultRegularFontName = $"TimesNewRoman-Regular{FontFileExtension}";
        private const string DefaultBoldFontName = $"TimesNewRoman-Bold{FontFileExtension}";
        private const string DefaultItalicFontName = $"TimesNewRoman-Italic{FontFileExtension}";
        private const string DefaultBoldItalicFontName = $"TimesNewRoman-BoldItalic{FontFileExtension}";
        private readonly ConcurrentDictionary<string, ITexture> textures = new ();
        private readonly IImageService imageService;
        private readonly ITextureFactory textureFactory;
        private readonly IFontAtlasService fontAtlasService;
        private readonly IFontMetaDataParser fontMetaDataParser;
        private readonly IPath path;
        private readonly IDisposable shutDownUnsubscriber;
        private readonly IReactable<DisposeTextureData> disposeTexturesReactable;
        private readonly string[] defaultFontNames =
        {
            DefaultRegularFontName, DefaultBoldFontName,
            DefaultItalicFontName, DefaultBoldItalicFontName,
        };
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureCache"/> class.
        /// </summary>
        /// <param name="imageService">Provides image related services.</param>
        /// <param name="textureFactory">Creates <see cref="ITexture"/> objects.</param>
        /// <param name="fontAtlasService">Provides font atlas services.</param>
        /// <param name="fontMetaDataParser">Parses metadata that might be attached to the file path.</param>
        /// <param name="path">Provides path related services.</param>
        /// <param name="shutDownReactable">Sends a push notifications that the application is shutting down.</param>
        /// <param name="disposeTexturesReactable">Sends push notifications to dispose of textures.</param>
        public TextureCache(
            IImageService imageService,
            ITextureFactory textureFactory,
            IFontAtlasService fontAtlasService,
            IFontMetaDataParser fontMetaDataParser,
            IPath path,
            IReactable<ShutDownData> shutDownReactable,
            IReactable<DisposeTextureData> disposeTexturesReactable)
        {
            EnsureThat.ParamIsNotNull(imageService);
            EnsureThat.ParamIsNotNull(textureFactory);
            EnsureThat.ParamIsNotNull(fontAtlasService);
            EnsureThat.ParamIsNotNull(fontMetaDataParser);
            EnsureThat.ParamIsNotNull(path);
            EnsureThat.ParamIsNotNull(shutDownReactable);
            EnsureThat.ParamIsNotNull(disposeTexturesReactable);

            this.imageService = imageService;
            this.textureFactory = textureFactory;
            this.fontAtlasService = fontAtlasService;
            this.fontMetaDataParser = fontMetaDataParser;
            this.path = path;
            this.shutDownUnsubscriber = shutDownReactable.Subscribe(new Reactor<ShutDownData>(
                _ => ShutDown(),
                onCompleted: () =>
                {
                    this.shutDownUnsubscriber?.Dispose();
                }));

            this.disposeTexturesReactable = disposeTexturesReactable;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TextureCache"/> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        ~TextureCache()
        {
            if (UnitTestDetector.IsRunningFromUnitTest)
            {
                return;
            }

            ShutDown();
        }

        /// <inheritdoc/>
        public int TotalCachedItems => this.textures.Count;

        /// <inheritdoc/>
        public ReadOnlyCollection<string> CacheKeys => new (this.textures.Keys.ToArray());

        /// <summary>
        /// Gets a texture using the given <paramref name="textureFilePath"/>.
        /// <para>
        ///     If the given <paramref name="textureFilePath"/> is a <c>.ttf</c> font file, then the texture will be
        ///     a font atlas texture created from the font.  Font file paths must include metadata.
        /// </para>
        /// </summary>
        /// <param name="textureFilePath">
        ///     The full file path to a <c>Texture(.png)</c> or <c>Font(.ttf)</c> file.
        /// </param>
        /// <returns>A standard texture or font atlas texture created from a font.</returns>
        /// <remarks>
        ///     If the item has not been previously created, the <see cref="TextureCache"/> class
        ///     will retrieve it, and then cache it for fast retrieval.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if the <paramref name="textureFilePath"/> is null or empty.
        /// </exception>
        /// <exception cref="CachingException">
        ///     Thrown for the following reasons:
        /// <list type="bullet">
        ///     <item>If a full file path is used with metadata that is not a <c>Font(.ttf)</c> file type.</item>
        ///     <item>If a <c>Font</c> path with metadata is not a fully qualified <c>Font</c> file path.</item>
        ///     <item>If a fully qualified <c>Font</c> file path has no metadata attached.</item>
        ///     <item>If a fully qualified <c>Texture</c> file path is not a <c>Texture(.png)</c> file type.</item>
        ///     <item>If a <c>Texture</c> path is not a fully qualified <c>Texture</c> file path.</item>
        /// </list>
        /// </exception>
        /// <exception cref="CachingMetaDataException">
        ///     Thrown if the metadata attached to the end of the path is invalid.
        /// </exception>
        public ITexture GetItem(string textureFilePath)
        {
            if (string.IsNullOrEmpty(textureFilePath))
            {
                throw new ArgumentNullException(nameof(textureFilePath), "The string parameter must not be null or empty.");
            }

            var parseResult = this.fontMetaDataParser.Parse(textureFilePath);

            string fullFilePath;
            var isFontFile = false;
            string cacheKey;
            string fileNameWithoutExtension;

            if (parseResult.ContainsMetaData)
            {
                // If the metadata is valid and the prefix is a full qualified file path
                if (parseResult.IsValid)
                {
                    if (parseResult.MetaDataPrefix.HasValidFullFilePathSyntax())
                    {
                        fileNameWithoutExtension = this.path.GetFileNameWithoutExtension(parseResult.MetaDataPrefix);

                        if (this.path.GetExtension(parseResult.MetaDataPrefix).ToLower() != FontFileExtension)
                        {
                            throw new CachingException($"Font caching must be a '{FontFileExtension}' file type.");
                        }

                        fullFilePath = parseResult.MetaDataPrefix;

                        // If the font file is a default font, tag it so it does not get unloaded
                        var defaultPrefix = this.defaultFontNames.Contains($"{fileNameWithoutExtension}{FontFileExtension}")
                            ? DefaultTag
                            : string.Empty;

                        cacheKey = $"{defaultPrefix}{parseResult.MetaDataPrefix}|size:{parseResult.FontSize}";
                        isFontFile = true;
                    }
                    else
                    {
                        var exceptionMsg = $"The font file path '{parseResult.MetaDataPrefix}' must be a";
                        exceptionMsg += $" fully qualified file path of type '{FontFileExtension}'.";
                        throw new CachingException(exceptionMsg);
                    }
                }
                else
                {
                    var exceptionMsg = $"The metadata '{parseResult.MetaData}' is invalid and is required";
                    exceptionMsg += $" for font files of type '{FontFileExtension}'.";
                    throw new CachingMetaDataException(exceptionMsg);
                }
            }
            else
            {
                if (textureFilePath.HasValidFullFilePathSyntax())
                {
                    fileNameWithoutExtension = this.path.GetFileNameWithoutExtension(textureFilePath);

                    var fileExtension = this.path.GetExtension(textureFilePath).ToLower();

                    if (fileExtension == FontFileExtension)
                    {
                        var exceptionMsg = "Font file paths must include metadata.";
                        exceptionMsg += $"{Environment.NewLine}Font Content Path MetaData Syntax: <file-path>|size:<font-size>";
                        exceptionMsg += $"{Environment.NewLine}Example: C:/Windows/Fonts/my-font.ttf|size:12";

                        throw new CachingMetaDataException(exceptionMsg);
                    }

                    if (fileExtension != TextureFileExtension)
                    {
                        throw new CachingException($"Texture caching must be a '{TextureFileExtension}' file type.");
                    }

                    fullFilePath = textureFilePath;
                    cacheKey = textureFilePath;
                }
                else
                {
                    var exceptionMsg = $"The texture file path '{textureFilePath}' must be a";
                    exceptionMsg += $" fully qualified file path of type '{TextureFileExtension}'.";
                    throw new CachingException(exceptionMsg);
                }
            }

            return this.textures.GetOrAdd(cacheKey, _ =>
            {
                ImageData imageData;

                if (isFontFile)
                {
                    var (atlasImageData, _) = this.fontAtlasService.CreateFontAtlas(fullFilePath, parseResult.FontSize);

                    atlasImageData = this.imageService.FlipVertically(atlasImageData);
                    imageData = atlasImageData;
                }
                else
                {
                    imageData = this.imageService.Load(fullFilePath);
                }

                var contentName = fileNameWithoutExtension;

                if (parseResult.ContainsMetaData)
                {
                    contentName = $"FontAtlasTexture|{contentName}|{parseResult.MetaData}";
                }

#if DEBUG
                AppStats.RecordLoadedFont(cacheKey);
#endif
                return this.textureFactory.Create(contentName, fullFilePath, imageData);
            });
        }

        /// <inheritdoc/>
        public void Unload(string cacheKey)
        {
            this.textures.TryRemove(cacheKey, out var texture);

            if (texture is not null)
            {
                this.disposeTexturesReactable.PushNotification(new DisposeTextureData(texture.Id));
#if DEBUG
                AppStats.ClearLoadedFont(cacheKey);
#endif
            }
        }

        /// <summary>
        /// Disposes of all textures.
        /// </summary>
        private void ShutDown()
        {
            if (this.isDisposed)
            {
                return;
            }

            var cacheKeys = this.textures.Keys.ToArray();

            // Dispose of all default and non default textures
            foreach (var cacheKey in cacheKeys)
            {
                this.textures.TryRemove(cacheKey, out var texture);

                if (texture is not null)
                {
                    this.disposeTexturesReactable.PushNotification(new DisposeTextureData(texture.Id));
                }
            }

            this.isDisposed = true;
        }
    }
}
