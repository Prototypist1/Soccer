using System;

namespace Common
{
    public struct CreateGame {
        public Guid Id { get; set; }

        public CreateGame(Guid id)
        {
            this.Id = id;
        }
    }
}
