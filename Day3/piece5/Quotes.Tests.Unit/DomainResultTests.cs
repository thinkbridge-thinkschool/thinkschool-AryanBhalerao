using FluentAssertions;
using QuotesApi.Models;
using Xunit;

namespace Quotes.Tests.Unit;

public class DomainResultTests
{
    [Fact]
    public void Ok_WithValue_IsSuccessIsTrue()
    {
        var result = DomainResult<string>.Ok("hello");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Ok_WithValue_ErrorIsNull()
    {
        var result = DomainResult<string>.Ok("hello");

        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_WithMessage_IsSuccessIsFalse()
    {
        var result = DomainResult<string>.Fail("something went wrong");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Fail_WithMessage_ValueIsNull()
    {
        var result = DomainResult<string>.Fail("something went wrong");

        result.Value.Should().BeNull();
    }
}
