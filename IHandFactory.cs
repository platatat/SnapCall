using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapCall
{
    public interface IHandFactory
    {
        IHand Create();
        IHand Create(IEnumerable<ICard> cards);
        IHand Create(ulong bitmap);
    }
}
