using Domain.Common;

namespace Domain.Tests.Common
{
    public class ResultTests
    {
        [Fact]
        public void Success_HasNoError()
        {
            var result = Result.Success();
            Assert.True(result.IsSuccess);
            Assert.Equal(Error.None, result.Error);
        }

        [Fact]
        public void Failure_CarriesError()
        {
            var error = new Error("Test.Error", "Something went wrong");
            var result = Result.Failure(error);
            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void GenericSuccess_ExposesValue()
        {
            var result = Result.Success(42);
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void GenericFailure_ThrowsWhenAccessingValue()
        {
            var result = Result.Failure<int>(new Error("Test.Error", "Something went wrong."));
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void ImplicitConversion_FromValue_CreatesSuccess()
        {
            Result<int> result = 42;
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }
    }
}
