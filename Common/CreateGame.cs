using System;

namespace Common
{
    public struct CreateGame {
        public string Id { get; set; }

        public CreateGame(string id)
        {
            this.Id = id;
        }
    }

    public struct GameCreated {
        public string Id { get; set; }

        public GameCreated(string id)
        {
            this.Id = id;
        }
    }


    public struct GameAlreadyExists
    {
        public string Id { get; set; }

        public GameAlreadyExists(string id)
        {
            this.Id = id;
        }
    }

    public struct GameDoesNotExist
    {
        public string Id { get; set; }

        public GameDoesNotExist(string id)
        {
            this.Id = id;
        }
    }


    public struct JoinGame
    {
        public string Id { get; set; }

        public JoinGame(string id)
        {
            this.Id = id;
        }
    }

    public struct GameJoined
    {
        public string Id { get; set; }

        public GameJoined(string id)
        {
            this.Id = id;
        }
    }
}
