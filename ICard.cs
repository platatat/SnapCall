using SnapCall.Enums;
using System;

namespace SnapCall
{
    public interface ICard
    {
        int PrimeRank { get; }
        int PrimeSuit { get; }
        Rank Rank { get; }
        Suit Suit { get; }
    }
}