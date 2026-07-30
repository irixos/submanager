using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SubManager.Api.Application.Features.Categories.Models;
using SubManager.Api.Application.Features.Channels.Models;
using Xunit;

namespace SubManager.Api.Tests.Application;

public sealed class ValidationTests
{
    [Theory]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData("Valid", "#12345")]
    [InlineData("Valid", "123456")]
    public void CreateCategory_InvalidNameOrColor_FailsValidation(string name, string? color)
    {
        var request = new CreateCategoryRequest { Name = name, Color = color };

        Assert.False(IsValid(request));
    }

    [Fact]
    public void CreateCategory_ValidValues_PassesValidation()
    {
        var request = new CreateCategoryRequest { Name = "Technology", Color = "#A1b2C3" };

        Assert.True(IsValid(request));
    }

    [Fact]
    public void CategoryNames_OverMaximumLength_FailCreateAndUpdateValidation()
    {
        var name = new string('x', 51);

        Assert.False(IsValid(new CreateCategoryRequest { Name = name }));
        Assert.False(IsValid(new UpdateCategoryRequest { Name = name }));
    }

    [Fact]
    public void UpdateCategory_InvalidColor_FailsValidation()
    {
        Assert.False(IsValid(new UpdateCategoryRequest { Color = "#12345" }));
    }

    [Fact]
    public void UpdateCategory_ClearColorAndColor_FailsValidation()
    {
        var request = new UpdateCategoryRequest { ClearColor = true, Color = "#123456" };

        var results = Validate(request);

        var error = Assert.Single(results);
        Assert.Contains(nameof(UpdateCategoryRequest.Color), error.MemberNames);
        Assert.Contains(nameof(UpdateCategoryRequest.ClearColor), error.MemberNames);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateChannel_MissingUrl_FailsValidation(string channelUrl)
    {
        Assert.False(IsValid(new CreateChannelRequest { ChannelUrl = channelUrl }));
    }

    [Theory]
    [InlineData("subscriptions.TXT", 5 * 1024 * 1024, true)]
    [InlineData("subscriptions.csv", 5 * 1024 * 1024 + 1, false)]
    [InlineData("subscriptions.xml", 10, false)]
    public void ImportChannels_FileBoundaries_AreValidated(string fileName, long length, bool expected)
    {
        var file = new FormFile(Stream.Null, 0, length, "file", fileName);
        var request = new ImportChannelsRequest { File = file };

        Assert.Equal(expected, IsValid(request));
    }

    [Fact]
    public void ImportChannels_MissingFile_FailsValidation()
    {
        Assert.False(IsValid(new ImportChannelsRequest()));
    }

    private static bool IsValid(object value) => Validate(value).Count == 0;

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        return results;
    }
}
