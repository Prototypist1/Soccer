using Common;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    interface IInputs
    {
        Task Init();
        Task<PlayerInputs> Next();
    }
}