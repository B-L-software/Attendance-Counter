using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance_Counter
{
    class report
    {
        public string Meeting_Date { get; set; }
        public string Attendance { get; set; }
        public string Users_Who_Took_Poll { get; set; }
        public string Poll_Result { get; set; }
        public string Failed_To_Take_Poll { get; set; }
        public string _ { get; set; }
        public string Missed_Meeting_MemberName { get; set; }
        public string M_M_UserName { get; set; }
        public string M_M_Email { get; set; }
        public string Service_Group { get; set; }
        public string Guests { get; set; }
        public string G_Email { get; set; }
    }
}
