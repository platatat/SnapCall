using System.Collections.Generic;

namespace SnapCall
{
    public class HandFactory : IHandFactory
    {
        public HandFactory() { }

        public IHand Create()
        {
            return new Hand();
        }

        public IHand Create(IEnumerable<ICard> cards)
        {
            return new Hand(cards);
        }

        public IHand Create(ulong bitmap)
        {
            return new Hand(bitmap);
        }
    }
}
