using ApiHoteleria.Models;
using ApiHoteleria.Services.Interfaces;

namespace ApiHoteleria.Services.Implementation
{
    public class IUsersImpl : IUsers
    {
        Task<bool> IUsers.create(Users user)
        {
            throw new NotImplementedException();
        }

        Task<bool> IUsers.delete(int id)
        {
            throw new NotImplementedException();
        }

        Task<Users> IUsers.find(int id)
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<Users>> IUsers.getAll()
        {
            throw new NotImplementedException();
        }

        Task<bool> IUsers.update(Users user)
        {
            throw new NotImplementedException();
        }
    }
}
