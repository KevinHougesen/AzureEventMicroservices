using Newtonsoft.Json;
namespace Data.Models;
public class EventModel
{
    public MailModel Mail { get; set; }
    public UserModel User { get; set; }

}