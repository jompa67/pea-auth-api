    // [Theory]
    // [InlineData("user@example.com", true)]
    // [InlineData("nonexistent@example.com", false)]
    // public async Task DoesEmailExistAsync_ReturnsExpectedResult(string email, bool expected)
    // {
    //     // Arrange
    //     if (expected)
    //     {
    //         _dbContext.Users.Add(new User { Email = email });
    //         await _dbContext.SaveChangesAsync();
    //     }
    //
    //     // Act
    //     var result = await _repository.DoesEmailExistAsync(email);
    //
    //     // Assert
    //     result.Should().Be(expected);
    // }
