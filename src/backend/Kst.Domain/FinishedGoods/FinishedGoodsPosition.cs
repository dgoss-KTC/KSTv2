using System.Globalization;
using System.Numerics;

namespace Kst.Domain.FinishedGoods;

public sealed record FinishedGoodsSourceRow(
    string Domain, string Site, string Product, string? Location, string? Lot, string? Reference,
    decimal OnHand, decimal UnitPrice, bool IsMissingPrice);

public sealed record FinishedGoodsPosition(string RowId, FinishedGoodsSourceRow Source)
{
    public string? Location => Source.Location?.ToUpperInvariant();
    public bool IsRma => Source.Lot?.StartsWith("RA", StringComparison.OrdinalIgnoreCase) == true;
    public decimal Value { get; } = FinishedGoodsDecimal.MultiplyExact(Source.OnHand, Source.UnitPrice);
}

/// <summary>Rejects decimal scale loss as well as overflow; decimal.TryParse/multiplication may otherwise round.</summary>
public static class FinishedGoodsDecimal
{
    private static readonly BigInteger MaxCoefficient = (BigInteger.One << 96) - 1;

    public static decimal ParseExact(string text)
    {
        if (string.IsNullOrEmpty(text)) throw new FormatException("Missing decimal value.");
        var negative = text[0] == '-';
        var digits = text[0] is '-' or '+' ? text[1..] : text;
        var pieces = digits.Split('.');
        if (pieces.Length > 2 || digits.Length == 0 ||
            pieces.Any(p => p.Any(c => c is < '0' or > '9')) ||
            pieces.All(p => p.Length == 0))
            throw new FormatException("Malformed decimal value.");
        var coefficient = BigInteger.Parse(string.Concat(pieces), CultureInfo.InvariantCulture);
        return FromCoefficient(negative ? -coefficient : coefficient, pieces.Length == 2 ? pieces[1].Length : 0);
    }

    public static decimal MultiplyExact(decimal left, decimal right)
    {
        var (a, scaleA) = Coefficient(left);
        var (b, scaleB) = Coefficient(right);
        return FromCoefficient(a * b, scaleA + scaleB);
    }

    private static (BigInteger Coefficient, int Scale) Coefficient(decimal value)
    {
        var bits = decimal.GetBits(value);
        var coefficient = (BigInteger)(uint)bits[0] | ((BigInteger)(uint)bits[1] << 32) | ((BigInteger)(uint)bits[2] << 64);
        return ((bits[3] & int.MinValue) != 0 ? -coefficient : coefficient, (bits[3] >> 16) & 0xff);
    }

    private static decimal FromCoefficient(BigInteger coefficient, int scale)
    {
        while (scale > 0 && coefficient % 10 == 0) { coefficient /= 10; scale--; }
        var absolute = BigInteger.Abs(coefficient);
        if (scale > 28 || absolute > MaxCoefficient) throw new OverflowException("Decimal value cannot be represented exactly.");
        return new decimal((int)(uint)(absolute & uint.MaxValue), (int)(uint)((absolute >> 32) & uint.MaxValue),
            (int)(uint)((absolute >> 64) & uint.MaxValue), coefficient.Sign < 0, (byte)scale);
    }
}
