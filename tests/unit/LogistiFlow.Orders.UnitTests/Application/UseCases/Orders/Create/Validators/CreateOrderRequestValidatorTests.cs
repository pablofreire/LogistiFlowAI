using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Validators;

namespace LogistiFlow.Orders.UnitTests.Application.UseCases.Orders.Create.Validators;

public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    [Fact]
    public void Validate_WellFormedRequest_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "IdempotencyKey")]
    [InlineData("key with spaces", "IdempotencyKey")]
    public void Validate_BadIdempotencyKey_IsInvalid(string key, string property)
    {
        var result = _validator.Validate(Valid() with { IdempotencyKey = key });

        result.Errors.Should().Contain(error => error.PropertyName == property);
    }

    [Theory]
    [InlineData("USA")]
    [InlineData("1A")]
    [InlineData("")]
    public void Validate_CountryNotIsoAlpha2_IsInvalid(string country)
    {
        _validator.Validate(Valid() with { DestinationCountry = country }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoLines_IsInvalid()
    {
        _validator.Validate(Valid() with { Lines = [] }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CreateOrderRequestValidator.MaxQuantityPerLine + 1)]
    public void Validate_QuantityOutOfRange_IsInvalid(int quantity)
    {
        _validator.Validate(Valid() with { Lines = [new CreateOrderLineRequest("SKU-1001", quantity)] }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SameSkuTwiceIgnoringCase_IsInvalid()
    {
        var request = Valid() with { Lines = [new("SKU-1001", 1), new("sku-1001", 2)] };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SkuWithInjectionCharacters_IsInvalid()
    {
        var request = Valid() with { Lines = [new("SKU'; DROP TABLE x;--", 1)] };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    private static CreateOrderRequest Valid() =>
        new("order-123", "Acme Retail", "Austin", "US", new DateOnly(2026, 11, 5), [new CreateOrderLineRequest("SKU-1001", 2)]);
}
