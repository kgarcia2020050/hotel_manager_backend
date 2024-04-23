using ApiHoteleria.Models;

namespace ApiHoteleria.Services.Interfaces
{
    public interface IUsers
    {
        Task<IEnumerable<Users>> getAll();
        Task<Users> find(int id);

        Task<bool> create(Users user);

        Task<bool> update(Users user);

        Task<bool> delete(int id);
    }
}
