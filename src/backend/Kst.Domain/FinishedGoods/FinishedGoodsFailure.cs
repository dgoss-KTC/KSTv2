namespace Kst.Domain.FinishedGoods;

public enum FinishedGoodsFailureReason
{
    Unknown, PriceEnvelopeInvalid, MissingPriceEnvelopeInvalid, PriceAllNull,
    PriceScopeIncomplete, InventoryScopeInvalid, InventoryQuantityInvalid, ApplicationSourceInvalid
}

public static class FinishedGoodsFailure
{
    public static InvalidDataException Create(FinishedGoodsFailureReason reason)
    {
        var error = new InvalidDataException("Finished Goods acquisition validation failed.");
        error.Data[typeof(FinishedGoodsFailureReason)] = reason;
        return error;
    }

    public static FinishedGoodsFailureReason Reason(Exception error) =>
        error.Data[typeof(FinishedGoodsFailureReason)] is FinishedGoodsFailureReason reason && Enum.IsDefined(reason)
            ? reason : FinishedGoodsFailureReason.Unknown;
}
