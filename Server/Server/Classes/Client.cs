using System;
using System.ComponentModel.DataAnnotations;

namespace Server.Classes
{
    public class Client
    {
        [Key]
        public int Id {  get; set; }
        public string Login {  get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public DateTime? DateConnect { get; set; }
        public bool IsBlackListed { get; set; }
    }
}