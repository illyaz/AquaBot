using System;

namespace AquaBot.Data
{
    public interface ICreatedTimestamp
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    public interface IUpdatedTimestamp
    {
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public interface ITimestamp : ICreatedTimestamp, IUpdatedTimestamp
    {
    }
}