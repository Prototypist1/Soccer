using System.Threading.Tasks;
using Common;

namespace RemoteSoccer
{
    interface IInputs
    {
        Task Init();
        Task<PlayerInputs> Next();
    }
}