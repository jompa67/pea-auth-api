        // using AuthApi.Contracts.Login;
        //
        // [Fact]
        // public async Task LoginWithPassword_WhenExceptionOccurs_ReturnsInternalServerError()
        // {
        //     // Arrange
        //     var request = new LoginRequest 
        //     { 
        //         Username = "validuser", 
        //         Password = "validpassword" 
        //     };
        //     
        //     _loginValidatorMock.ValidateAsync(request, Arg.Any<CancellationToken>())
        //         .Returns(new ValidationResult());
        //         
        //     _loginServiceMock.LoginWithPassword(request, Arg.Any<CancellationToken>())
        //         .Throws(new Exception("Test exception"));
        //         
        //     // Act
        //     var result = await _controller.LoginWithPassword(request);
        //     
        //     // Assert
        //     var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        //     statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        // }
