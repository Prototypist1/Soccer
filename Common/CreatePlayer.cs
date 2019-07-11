using System;

namespace Common
{
    public struct CreatePlayer {
        public readonly Guid Id;

        public CreatePlayer(Guid id)
        {
            Id = id;
        }
    }
}
