using System;

namespace Common
{
    public struct CreateGame {
        public readonly Guid id;

        public CreateGame(Guid id)
        {
            this.id = id;
        }
    }
}
