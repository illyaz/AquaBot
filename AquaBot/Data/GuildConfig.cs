using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaBot.Data
{
    public class GuildConfig : IEntity<ulong>, ITimestamp
    {
        public ulong Id { get; set; }
        
        [MaxLength(3)]
        public string? Prefix { get; set; }
        
        [Column(TypeName = "jsonb")] public MusicConfig? Music { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public class MusicConfig
        {
            public ulong? DjRoleId { get; set; }
            public short DefaultVolume { get; set; } = 100;
            public bool PreventDuplicates { get; set; } = true;
            public bool DeleteUserCommand { get; set; } = false;
        }
    }
}