using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StackExchange.Opserver
{
    public class UserSettings : Settings<UserSettings>, IAfterLoadActions
    {
        public override bool Enabled { get { return Users.Any(); } }

        public ObservableCollection<User> Users { get; set; }
        public event EventHandler<User> UserAdded = delegate { };
        public event EventHandler<List<User>> UsersChanged = delegate { };
        public event EventHandler<User> UserRemoved = delegate { };
      

        public UserSettings()
        {
            Users = new ObservableCollection<User>();
        }

        public void AfterLoad()
        {
            Users.AddHandlers(this, UserAdded, UsersChanged, UserRemoved);
        }

        /// <summary>
        /// The default connection string to use when connecting to servers, $ServerName$ will be parameterized
        /// </summary>
        public string DefaultConnectionString { get; set; }

        public class User : IAfterLoadActions, ISettingsCollectionItem<User>
        {

            public ObservableCollection<Group> Groups
            {
                get;
                set;
            }
            public event EventHandler<Group> GroupAdded = delegate
            {
            };
            public event EventHandler<List<Group>> GroupsChanged = delegate
            {
            };
            public event EventHandler<Group> GroupRemoved = delegate
            {
            };

            public User()
            {
                Groups = new ObservableCollection<Group>();
            }

            public void AfterLoad()
            {
                Groups.AddHandlers(this, GroupAdded, GroupsChanged, GroupRemoved);
            }

            /// <summary>
            /// The friendly name for this User
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The login username for this User
            /// </summary>
            public string Username
            {
                get;
                set;
            }
            
            //TODO: provide some sort of hashing where passwords can be created in plain text but get updated to hashed automatically
            /// <summary>
            /// The [plain text] login password for this User
            /// </summary>
            public string Password
            {
                get;
                set;
            }
           

            public bool Equals(User other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                       && string.Equals(Username, other.Username)
                       && string.Equals(Password, other.Password);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((User) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Username != null ? Username.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Password != null ? Password.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(User left, User right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(User left, User right)
            {
                return !Equals(left, right);
            }
        }
        public class Group : ISettingsCollectionItem<Group>
        {
            /// <summary>
            /// The Group name that this user is a member of
            /// </summary>
            public string Name
            {
                get;
                set;
            }

            public bool Equals(Group other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                return string.Equals(Name, other.Name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((Group)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name != null ? Name.GetHashCode() : 0) * 397);
                }
            }
        }
    }
}
