        private IVerificationTokenService _verificationTokenServiceMock;
        
        [SetUp]
        public void Setup()
        {
            _loggerMock = Substitute.For<ILogger<AuthController>>();
            _loginServiceMock = Substitute.For<ILoginService>();
            _loginValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.Login.LoginRequest>>();
            _registerValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest>>();
            _refreshTokenValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.RefreshTokenRequest>>();
            _verificationTokenServiceMock = Substitute.For<IVerificationTokenService>();

            _controller = new AuthController(
                _loggerMock,
                _loginServiceMock,
                _loginValidatorMock,
                _registerValidatorMock,
                _refreshTokenValidatorMock,
                _verificationTokenServiceMock
            );
        }
