﻿// <copyright file="OpenGLInitReactableTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTests.Reactables
{
    using Moq;
    using Velaptor.Reactables;
    using Velaptor.Reactables.Core;
    using Velaptor.Reactables.ReactableData;
    using Xunit;

    /// <summary>
    /// Tests the <see cref="OpenGLInitReactable"/> class.
    /// </summary>
    public class OpenGLInitReactableTests
    {
        #region Method Tests
        [Fact]
        public void PushNotification_WhenInvoked_SendsPushNotification()
        {
            // Arrange
            var reactor = new Mock<IReactor<GLInitData>>();

            var reactable = new OpenGLInitReactable();
            reactable.Subscribe(reactor.Object);

            // Act
            reactable.PushNotification(default);

            // Assert
            reactor.Verify(m => m.OnNext(default), Times.Once());
        }
        #endregion
    }
}
