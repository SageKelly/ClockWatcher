using System;

namespace TimeKeeper
{
    public class User
    {
        public Guid UserID
        {
            get; set;
        }

        public DateTimeOffset Created
        {
            get; set;
        }

        public string FirstName
        {
            get; set;
        }

        public string LastName
        {
            get; set;
        }

        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }

            private set { }
        }



    }
}
