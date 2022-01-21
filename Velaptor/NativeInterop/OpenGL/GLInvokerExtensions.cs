﻿// <copyright file="GLInvokerExtensions.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.NativeInterop.OpenGL
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Numerics;
    using Velaptor.OpenGL;

    /// <summary>
    /// Provides extensions/helper methods to improve OpenGL related functionality.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class GLInvokerExtensions : IGLInvokerExtensions
    {
        private readonly IGLInvoker glInvoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="GLInvokerExtensions"/> class.
        /// </summary>
        /// <param name="glInvoker">Invokes OpenGL functions.</param>
        public GLInvokerExtensions(IGLInvoker glInvoker) => this.glInvoker = glInvoker;

        /// <inheritdoc/>
        public Size GetViewPortSize()
        {
            /*
             * [0] = X
             * [1] = Y
             * [3] = Width
             * [4] = Height
             */
            var data = new int[4];

            this.glInvoker.GetInteger(GLGetPName.Viewport, data);

            return new Size(data[2], data[3]);
        }

        /// <inheritdoc/>
        public void SetViewPortSize(Size size)
        {
            /*
             * [0] = X
             * [1] = Y
             * [3] = Width
             * [4] = Height
             */
            var data = new int[4];

            this.glInvoker.GetInteger(GLGetPName.Viewport, data);

            this.glInvoker.Viewport(data[0], data[1], (uint)size.Width, (uint)size.Height);
        }

        /// <inheritdoc/>
        public Vector2 GetViewPortPosition()
        {
            /*
           * [0] = X
           * [1] = Y
           * [3] = Width
           * [4] = Height
           */
            var data = new int[4];

            this.glInvoker.GetInteger(GLGetPName.Viewport, data);

            return new Vector2(data[0], data[1]);
        }

        /// <inheritdoc/>
        public bool LinkProgramSuccess(uint program)
        {
            this.glInvoker.GetProgram(program, GLProgramParameterName.LinkStatus, out var programParams);

            return programParams >= 1;
        }

        /// <inheritdoc/>
        public bool ShaderCompileSuccess(uint shaderId)
        {
            this.glInvoker.GetShader(shaderId, GLShaderParameter.CompileStatus, out var shaderParams);

            return shaderParams >= 1;
        }

        /// <inheritdoc/>
        public void BeginGroup(string label)
                    =>
                        this.glInvoker.PushDebugGroup(
                            GLDebugSource.DebugSourceApplication,
                            100,
                            (uint)label.Length,
                            label);

        /// <inheritdoc/>
        public void EndGroup() => this.glInvoker.PopDebugGroup();

        /// <inheritdoc/>
        public void LabelShader(uint shaderId, string label)
            => this.glInvoker.ObjectLabel(GLObjectIdentifier.Shader, shaderId, (uint)label.Length, label);

        /// <inheritdoc/>
        public void LabelShaderProgram(uint shaderId, string label)
            => this.glInvoker.ObjectLabel(GLObjectIdentifier.Program, shaderId, (uint)label.Length, label);

        /// <inheritdoc/>
        public void LabelVertexArray(uint vertexArrayId, string label)
        {
            label = string.IsNullOrEmpty(label)
                ? "NOT SET"
                : label;

            var newLabel = $"{label} VAO";

            this.glInvoker.ObjectLabel(GLObjectIdentifier.VertexArray, vertexArrayId, (uint)newLabel.Length, newLabel);
        }

        /// <inheritdoc/>
        public void LabelBuffer(uint bufferId, string label, BufferType bufferType)
        {
            label = string.IsNullOrEmpty(label)
                ? "NOT SET"
                : label;

            var bufferTypeAcronym = bufferType switch
            {
                BufferType.VertexBufferObject => "VBO",
                BufferType.IndexArrayObject => "EBO",
                _ => throw new ArgumentOutOfRangeException(nameof(bufferType), bufferType, null)
            };

            var newLabel = $"{label} {bufferTypeAcronym}";

            this.glInvoker.ObjectLabel(GLObjectIdentifier.Buffer, bufferId, (uint)newLabel.Length, newLabel);
        }

        /// <inheritdoc/>
        public void LabelTexture(uint textureId, string label)
        {
            label = string.IsNullOrEmpty(label)
                ? "NOT SET"
                : label;

            this.glInvoker.ObjectLabel(GLObjectIdentifier.Texture, textureId, (uint)label.Length, label);
        }
    }
}
