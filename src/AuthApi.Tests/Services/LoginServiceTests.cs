            _userProfileRepositoryMock.GetByUsernameAsync("newuser").Returns(Task.FromResult<UserProfile>(null));
