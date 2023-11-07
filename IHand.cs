using System.Collections.Generic;

namespace SnapCall
{
    public interface IHand
    {
        IList<Card> Cards { get; set; }

        HandStrength GetStrength();
    }
}