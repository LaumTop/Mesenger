using System.ComponentModel.DataAnnotations;

namespace Masanger;

public class Users
{
    public string nickname;
    [Key]
    public int id;

    public Users(String _nickname)
    {
        nickname = _nickname;
    }
}