using System.Collections.Generic;

namespace SnapCall
{
    public interface IHand : IEnumerable<ICard>
    {
        HandStrength GetStrength();
    }
}