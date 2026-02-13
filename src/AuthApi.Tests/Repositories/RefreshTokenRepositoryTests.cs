using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AuthApi.Models;
using AuthApi.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AuthApi.Tests.Repositories
{
    public class RefreshTokenRepositoryTests
    {
        private readonly RefreshTokenRepository _repository;
        private readonly IAmazonDynamoDB _dynamoDbMock;
        private readonly ILogger<RefreshTokenRepository> _loggerMock;
        private readonly DynamoDBContext _contextMock;

        public RefreshTokenRepositoryTests()
        {
            _dynamoDbMock = Substitute.For<IAmazonDynamoDB>();
            _loggerMock = Substitute.For<ILogger<RefreshTokenRepository>>();
            _contextMock = Substitute.For<DynamoDBContext>(_dynamoDbMock);
            
            // Use constructor-based injection for testing
            _repository = new TestRefreshTokenRepository(_contextMock, _loggerMock);
        }

        [Fact]
        public async Task RevokeAllForJwtTokenAsync_ValidJwtToken_ReturnsTrue()
        {
            // Arrange
            var jwtToken = "valid.jwt.token";
            var tokenList = new List<RefreshTokenData>
            {
                new RefreshTokenData 
                { 
                    Id = "id1", 
                    RefreshToken = "refreshToken1", 
                    JwtToken = jwtToken,
                    IsRevoked = false 
                },
                new RefreshTokenData 
                { 
                    Id = "id2", 
                    RefreshToken = "refreshToken2", 
                    JwtToken = jwtToken,
                    IsRevoked = false 
                }
            };
            
            // Mock the async enumerable returned by QueryAsync
            var asyncEnumerable = Substitute.For<AsyncSearch<RefreshTokenData>>();
            asyncEnumerable.GetRemainingAsync().Returns(tokenList);
            
            _contextMock.QueryAsync<RefreshTokenData>(
                Arg.Any<object>(), 
                Arg.Any<DynamoDBOperationConfig>()
            ).Returns(asyncEnumerable);
            
            // Mock the SaveAsync method to do nothing
            _contextMock.SaveAsync(Arg.Any<RefreshTokenData>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _repository.RevokeAllForJwtTokenAsync(jwtToken);

            // Assert
            Assert.True(result);
            
            // Verify that all tokens were marked as revoked
            Assert.All(tokenList, token => Assert.True(token.IsRevoked));
            
            // Verify that SaveAsync was called for each token
            await _contextMock.Received(tokenList.Count).SaveAsync(
                Arg.Any<RefreshTokenData>(), 
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task RevokeAllForJwtTokenAsync_NoTokensFound_ReturnsTrue()
        {
            // Arrange
            var jwtToken = "jwt.with.no.tokens";
            var emptyList = new List<RefreshTokenData>();
            
            var asyncEnumerable = Substitute.For<AsyncSearch<RefreshTokenData>>();
            asyncEnumerable.GetRemainingAsync().Returns(emptyList);
            
            _contextMock.QueryAsync<RefreshTokenData>(
                Arg.Any<object>(), 
                Arg.Any<DynamoDBOperationConfig>()
            ).Returns(asyncEnumerable);

            // Act
            var result = await _repository.RevokeAllForJwtTokenAsync(jwtToken);

            // Assert
            Assert.True(result);
            
            // Verify SaveAsync was not called since there were no tokens
            await _contextMock.DidNotReceive().SaveAsync(
                Arg.Any<RefreshTokenData>(), 
                Arg.Any<CancellationToken>()
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task RevokeAllForJwtTokenAsync_NullOrEmptyJwtToken_ReturnsFalse(string jwtToken)
        {
            // Act
            var result = await _repository.RevokeAllForJwtTokenAsync(jwtToken);

            // Assert
            Assert.False(result);
            
            // Verify that QueryAsync and SaveAsync were not called
            _contextMock.DidNotReceive().QueryAsync<RefreshTokenData>(
                Arg.Any<object>(), 
                Arg.Any<DynamoDBOperationConfig>()
            );
            
            await _contextMock.DidNotReceive().SaveAsync(
                Arg.Any<RefreshTokenData>(), 
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task RevokeAllForJwtTokenAsync_QueryThrowsException_PropagatesException()
        {
            // Arrange
            var jwtToken = "valid.jwt.token";
            var expectedException = new Exception("Query failed");
            
            _contextMock.QueryAsync<RefreshTokenData>(
                Arg.Any<object>(), 
                Arg.Any<DynamoDBOperationConfig>()
            ).Throws(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _repository.RevokeAllForJwtTokenAsync(jwtToken)
            );
            
            Assert.Same(expectedException, exception);
        }

        [Fact]
        public async Task RevokeAllForJwtTokenAsync_SaveThrowsException_PropagatesException()
        {
            // Arrange
            var jwtToken = "valid.jwt.token";
            var tokenList = new List<RefreshTokenData>
            {
                new RefreshTokenData 
                { 
                    Id = "id1", 
                    RefreshToken = "refreshToken1", 
                    JwtToken = jwtToken,
                    IsRevoked = false 
                }
            };
            
            var asyncEnumerable = Substitute.For<AsyncSearch<RefreshTokenData>>();
            asyncEnumerable.GetRemainingAsync().Returns(tokenList);
            
            _contextMock.QueryAsync<RefreshTokenData>(
                Arg.Any<object>(), 
                Arg.Any<DynamoDBOperationConfig>()
            ).Returns(asyncEnumerable);
            
            var saveException = new Exception("Save failed");
            _contextMock.SaveAsync(Arg.Any<RefreshTokenData>(), Arg.Any<CancellationToken>())
                .Throws(saveException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _repository.RevokeAllForJwtTokenAsync(jwtToken)
            );
            
            Assert.Same(saveException, exception);
            
            // Verify that the token was marked as revoked before attempting to save
            Assert.True(tokenList[0].IsRevoked);
        }
    }
    
    // Helper class to allow dependency injection of the mocked context
    // This avoids having to use reflection to replace the private field
    public class TestRefreshTokenRepository : RefreshTokenRepository
    {
        public TestRefreshTokenRepository(DynamoDBContext context, ILogger<RefreshTokenRepository> logger)
            : base(Substitute.For<IAmazonDynamoDB>(), logger)
        {
            // Use reflection to set the private _context field with our mock
            var contextField = typeof(RefreshTokenRepository).GetField("_context", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            contextField.SetValue(this, context);
        }
    }
}
