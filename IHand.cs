using System.Collections.Generic;

namespace SnapCall
{
    public interface IHand
    {
        IList<ICard> Cards { get; set; }

        HandStrength GetStrength();
    }
}