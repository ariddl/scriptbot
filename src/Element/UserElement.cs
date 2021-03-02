using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordScriptBot.Element
{
    public class UserElement : IElement
    {
        public string ElementName => "user";
        private IGuildUser _user;

        public UserElement(IGuildUser user)
        {
            _user = user;
        }

        public async void SendDM(string text) =>
            await (await _user.GetOrCreateDMChannelAsync()).SendMessageAsync(text);

        [ElementInterface("kick", "Kick the user from the server.")]
        public async void Kick() => await _user.KickAsync();

        [ElementInterface("ban", "Ban the user from the server.")]
        public async void Ban(int pruneDays, string reason) =>
            await _user.BanAsync(pruneDays, reason);

        [ElementInterface("username", "This user's username.")]
        public StringElement Username => new StringElement(_user.Username);

        [ElementInterface("nickname", "This user's nickname in the server.")]
        public StringElement Nickname => new StringElement(_user.Nickname);

        [ElementInterface("hasRole", "If the user has a role with the name specified.")]
        public bool HasRole(string name)
        {
            foreach (ulong roleId in _user.RoleIds)
            {
                IRole role = _user.Guild.GetRole(roleId);
                if (role.Name == name)
                    return true;
            }
            return false;
        }
    }
}
