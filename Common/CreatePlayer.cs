using System;

namespace Common
{
    public struct CreatePlayer {
        public readonly Guid foot;
        public readonly Guid body;

        public CreatePlayer(Guid foot, Guid body)
        {
            this.foot = foot;
            this.body = body;
        }
    }
}
