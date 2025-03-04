using System.Collections.Generic;
using TeamCitySharp.DomainEntities;

namespace TeamCitySharp.ActionTypes
{
  public interface IUsers
  {
    List<User> All();
    Users GetFields(string fields);
    User Details(string userName);
    List<Role> AllRolesByUserName(string userName);
    List<Group> AllGroupsByUserName(string userName);
    List<Group> AllUserGroups();
    List<User> AllUsersByUserGroup(string userGroupName);
    List<Role> AllUserRolesByUserGroup(string userGroupName);
    bool Create(string username, string name, string email, string password);
    void Delete(string username);
    bool AddPassword(string username, string password);
    bool IsAdministrator(string username);
  }
}
