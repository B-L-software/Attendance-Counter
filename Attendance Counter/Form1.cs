using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Attendance_Counter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //vars for app
        //const char groupSep = (char)29;
        //const char recSep = (char)30;
        //const char unitSep = (char)31;
        string cfgPath = Application.StartupPath + @"/servgroups.cfg";
        string participants = "";
        string poll = "";
        List<report> reports = new List<report>();
        List<string> nonpollers = new List<string>();
        //string reportfn =  DateTime.Today.ToString("MM-dd-yyyy") + "_Report.csv";
        


        //stucts
        public struct PollTaker
        {
            public string GroupName;
            public string MemberName;
            public string Email;
            public string Username;
            public string PollResult;
        }
        HashSet<PollTaker> pollTakers = new HashSet<PollTaker>();
       
        public struct Absentee
        {
            public string GroupName;
            public string MemberName;
            public string Email;
            public string Username;

            public Absentee(string GroupName, string MemberName) : this()
            {
                this.GroupName = GroupName;
                this.MemberName = MemberName;
            }
        }
        HashSet<Absentee> absentees = new HashSet<Absentee>();

        public struct Guest
        {
            public string Name;
            public string Email;
            
            public Guest(string Name) : this()
            {
                this.Name = Name;
            }
        }
        HashSet<Guest> guests = new HashSet<Guest>();


        private void btnAddGroup_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtServiceGroup.Text)) { return; }
                if (string.IsNullOrWhiteSpace(txtMN.Text)) { return; }

                bool memberadded = false;
                //determine if group exists and add to it otherwise make new group
                foreach(TreeNode nd in tvSG.Nodes)
                {
                    if (nd.Text == txtServiceGroup.Text.Trim())
                    {
                        //group exists so add it to this node
                        memberadded = true;
                        TreeNode nnd = nd.Nodes.Add("Member Name:" + txtMN.Text.Trim());
                        nnd.Nodes.Add("Emails:" + txtEM.Text.Trim());
                        nnd.Nodes.Add("Usernames:" + txtUN.Text.Trim());

                    }
                    
                }
                if (!memberadded)
                {
                    //Group didnt exist so create it and add member
                    TreeNode nd = tvSG.Nodes.Add(txtServiceGroup.Text.Trim());
                    TreeNode nnd = nd.Nodes.Add("Member Name:" + txtMN.Text.Trim());
                    nnd.Nodes.Add("Emails:" + txtEM.Text.Trim());
                    nnd.Nodes.Add("Usernames:" + txtUN.Text.Trim());

                }

                //save tree
                SaveTree(tvSG, cfgPath);
                txtEM.Clear();
                txtMN.Clear();
                txtUN.Clear();

            }
            catch (Exception ex)
            {
                MessageBox.Show("btnAddGroup_Click\n" + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadServiceGroups();

        }

        private void LoadServiceGroups()
        {
            try
            {
                //we save it in an ini file
                if (File.Exists(cfgPath))
                {
                    //open ini file
                    LoadTree(tvSG, cfgPath);

                }
               
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadServiceGroups\n" + ex.Message);
            }
        }


        public static void SaveTree(TreeView tree, string filename)
        {
            try
            {
                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(file, tree.Nodes.Cast<TreeNode>().ToList());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SaveTree\n" + ex.Message);
            }
        }

        public static void LoadTree(TreeView tree, string filename)
        {
            try
            {
                using (Stream file = File.Open(filename, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    object obj = bf.Deserialize(file);

                    TreeNode[] nodeList = (obj as IEnumerable<TreeNode>).ToArray();
                    tree.Nodes.AddRange(nodeList);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadTree\n" + ex.Message);
            }
        }

        public string threePointSearch(ref string strToSearch, string LocatorKey, string Key1, string Key2)
        {
            try
            {
                //find LocatorKey string and then the start and end location of the next two keys after this point
                /*Say we have the following multiline string:
                 * "<p>Owner:</p>
                 * <p>Someone
                 * 156 suchnsuch street </p>
                 * <p>Taxpayer:</p>
                 * <p>Someone Else
                 * 3601 suchnsuch ave </p>"
                 * 
                 * We want to find the taxpayer info so our LocatorKey would be "<p>Taxpayer:</p>"
                 * Then our next search key would be "<p>" and the last key would be "</p>"
                 * This function returns everything between the last two strings or nothing if nothing is found
                 */
                int loc_start = 0;
                int loc_end = 0;

                loc_start = strToSearch.IndexOf(LocatorKey);
                if (loc_start > -1)
                {
                    loc_start = strToSearch.IndexOf(Key1, loc_start) + Key1.Length;
                    loc_end = strToSearch.Substring(loc_start).IndexOf(Key2);

                    if (loc_end > -1)
                    {
                        return strToSearch.Substring(loc_start, loc_end).Replace(@",", " ");
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("threePointSearch\n" + ex.Message);
                return "";
            }
        }

        private string SearchbyParams(ref string stringtosearch, ref int startIndex, string startString, string stopString)
        {
            try
            {
                /*performs a search of a string and returns the result between two values
                the startIndex will return -1 if the search fails
                otherwise the startIndex will be updated to the location at the end of the found parameters
                this will allow another search to be peformed at the next index  */
                int loc_start = 0;
                if (startString != "") //if the startstring is blank we just start at position 0
                {
                    loc_start = stringtosearch.IndexOf(startString, startIndex);
                }

                startIndex = -1;
                if (loc_start > -1)
                {
                    loc_start += startString.Length;
                    int loc_end = stringtosearch.Substring(loc_start).IndexOf(stopString);

                    if (loc_end > -1)
                    {
                        startIndex = loc_start + loc_end + stopString.Length;
                        return stringtosearch.Substring(loc_start, loc_end);
                    }
                    else
                    {
                        return "";
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("SearchbyParams\n" + ex.Message);
                startIndex = -1;
                return "";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void tvSG_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                txtMN.Clear();
                txtEM.Clear();
                txtUN.Clear();

                if (tvSG.SelectedNode != null)
                {
                    if (tvSG.SelectedNode.Parent == null)
                    {
                        //node is root so it is a service group node
                        txtServiceGroup.Text = tvSG.SelectedNode.Text;
                        //nothing more to do here because we need to select the member name to
                        //get all the info
                    }
                    else if(tvSG.SelectedNode.Parent.Parent == null)
                    {
                        //this is the member name because it is the second tier node
                        txtServiceGroup.Text = tvSG.SelectedNode.Parent.Text;
                        txtMN.Text = tvSG.SelectedNode.Text.Replace("Member Name:","");
                        foreach(TreeNode node in tvSG.SelectedNode.Nodes)
                        {
                            if (node.Text.IndexOf("Emails:") > -1)
                            {
                                txtEM.Text = node.Text.Replace("Emails:","");
                            }
                            else
                            {
                                txtUN.Text = node.Text.Replace("Usernames:", "");
                            }
                        }
                    }
                    else if (tvSG.SelectedNode.Parent.Parent.Parent == null)
                    {
                        tvSG.SelectedNode = tvSG.SelectedNode.Parent;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("tvSG_AfterSelect\n" + ex.Message);
            }
        }

        private void btnUpdateGroup_Click(object sender, EventArgs e)
        {
            try
            {
                if (tvSG.SelectedNode != null)
                {
                    if (tvSG.SelectedNode.Parent == null)
                    {
                        //node is root so it is a service group node
                        tvSG.SelectedNode.Text = txtServiceGroup.Text;
                        //nothing more to do here because we need to select the member name to
                        //get all the info
                    }
                    else if (tvSG.SelectedNode.Parent.Parent == null)
                    {
                        //this is the member name because it is the second tier node
                        tvSG.SelectedNode.Parent.Text = txtServiceGroup.Text;
                        tvSG.SelectedNode.Text = "Member Name:" + txtMN.Text;
                        foreach (TreeNode node in tvSG.SelectedNode.Nodes)
                        {
                            if (node.Text.IndexOf("Emails:") > -1)
                            {
                                tvSG.SelectedNode.Nodes[node.Index].Text = "Emails:" + txtEM.Text;
                            }
                            else
                            {
                                tvSG.SelectedNode.Nodes[node.Index].Text = "Usernames:" + txtUN.Text;
                            }
                        }
                    }

                    //save tree
                    SaveTree(tvSG, cfgPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnUpdateGroup_Click\n" + ex.Message);
            }
        }

        private void btnRemoveGroupElement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tvSG.SelectedNode != null)
                {
                    if (tvSG.SelectedNode.Parent == null)
                    {
                        //node is root so it is a service group node
                        if (MessageBox.Show("Are you sure you want to remove:\n" + tvSG.SelectedNode.Text + "?", "Remove Group?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            tvSG.SelectedNode.Remove();
                        }
                        
                    }
                    else if (tvSG.SelectedNode.Parent.Parent == null)
                    {
                        //this is the member name because it is the second tier node
                        if (MessageBox.Show("Are you sure you want to remove Member:\n" + tvSG.SelectedNode.Text + "?", "Remove Member?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            tvSG.SelectedNode.Remove();
                        }

                    }

                    //save tree
                    SaveTree(tvSG, cfgPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnRemoveGroupElement_Click\n" + ex.Message);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                dlgOF.DefaultExt = "cfg";
                dlgOF.Filter = "Config Files (*.cfg)|*.cfg|All Files (*.*)|*.*";
                if (dlgOF.ShowDialog() == DialogResult.OK)
                {
                    tvSG.Nodes.Clear();
                    LoadTree(tvSG, dlgOF.FileName);
                    SaveTree(tvSG, cfgPath);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("btnImport_Click\n" + ex.Message);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                dlgSF.Filter = "Config Files (*.cfg)|*.cfg|All Files (*.*)|*.*";
                dlgSF.DefaultExt = "cfg";
                if (dlgSF.ShowDialog() == DialogResult.OK)
                {
                    SaveTree(tvSG, dlgSF.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnExport_Click\n" + ex.Message);
            }
        }

        private void btnLoadParticipantsList_Click(object sender, EventArgs e)
        {
            try
            {
                dlgOF.DefaultExt = "csv";
                dlgOF.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                if (dlgOF.ShowDialog() == DialogResult.OK)
                {
                    lstbxEmail.Items.Clear();
                    lstbxName.Items.Clear(); 
                    absentees.Clear();
                    guests.Clear();
                    participants = File.ReadAllText(dlgOF.FileName);
                    //fill the listboxes
                    //read line by line of the csv file
                    string[] readline = participants.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    int datestart = 0;
                    bool start = false;
                    
                    if (!string.IsNullOrWhiteSpace(txtHost.Text))
                    {
                        lstbxName.Items.Add(txtHost.Text.Trim());
                        lstbxEmail.Items.Add("");
                    }
                    
                    foreach (string line in readline)
                    {
                        //divide the line by comma seperated values
                        string[] csv = line.Split(',');
                        Guest guest = new Guest();
                        if (start)
                        {
                            if (csv.Length < 2)
                            {
                                continue;
                            }
                            if (!string.IsNullOrWhiteSpace(csv[0]))
                            {
                                lstbxName.Items.Add(csv[0]);
                                guest.Name = csv[0];
                            }
                            else
                            {
                                lstbxName.Items.Add("");
                            }
                            if (!string.IsNullOrWhiteSpace(csv[1]))
                            {
                                lstbxEmail.Items.Add(csv[1]);
                                guest.Email = csv[1];
                            }
                            else
                            {
                                lstbxEmail.Items.Add("");
                            }
                            //add the guest to the guests list
                            guests.Add(guest);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(csv[0]))
                            {
                                //find starting point
                                if (!string.IsNullOrWhiteSpace(csv[0]))
                                {
                                    datestart++;
                                    if (datestart == 2)
                                    {
                                        if (csv.Length < 3)
                                        {
                                            continue;
                                        }
                                        txtPartDate.Text = DateTime.Parse(csv[2]).ToString("MM/dd/yyyy");
                                    }
                                    if (csv[0].IndexOf(Properties.Settings.Default.PartStart) > -1)
                                    {
                                        start = true;
                                        lstbxName.Items.Clear();
                                        lstbxEmail.Items.Clear();
                                    }
                                }
                            }
                        }
                    }
                }

                //go thru each service group and find any matches to username and emails
                foreach (TreeNode gNode in tvSG.Nodes) //group name
                {

                    foreach (TreeNode mNode in gNode.Nodes)//member name
                    {
                        //add member to absentee hashset list and remove if found later
                        //a hashset is used because it only adds if it's unique
                        Absentee a = new Absentee(gNode.Text, mNode.Text);
                        Guest guest = new Guest();

                        foreach (TreeNode ueNode in mNode.Nodes)//username and email
                        {                            
                            //Usernames and Emails are seperated by commas
                            if (ueNode.Text.IndexOf("Usernames:") > -1)
                            {
                                a.Username = ueNode.Text;
                                guest.Name = ueNode.Text;
                                foreach (string un in lstbxName.Items)
                                {
                                    if (string.IsNullOrWhiteSpace(un))
                                    {
                                        continue;
                                    }
                                    if (ueNode.Text.IndexOf(un) > -1)
                                    {
                                        //remove from absentee list because it was found
                                        a.MemberName = "";
                                        
                                        break;
                                    }
                                }
                            }
                            if (ueNode.Text.IndexOf("Emails:") > -1)
                            {
                                a.Email = ueNode.Text;
                                guest.Email = ueNode.Text;
                                foreach (string em in lstbxEmail.Items)
                                {
                                    if (string.IsNullOrWhiteSpace(em))
                                    {
                                        continue;
                                    }
                                    if (ueNode.Text.IndexOf(em) > -1)
                                    {
                                        //remove from absentee list because it was found
                                        a.MemberName = "";
                                        break;
                                    }
                                }
                            }                            
                        }
                        //add absentee if it isn't blank
                        if (!string.IsNullOrEmpty(a.MemberName))
                        {
                            a.MemberName = a.MemberName.Replace("Member Name:", "");
                            a.Username = a.Username.Replace("Usernames:", "");
                            a.Email = a.Email.Replace("Emails:", "");
                            absentees.Add(a);
                        }
                        //remove from guest list if name is valid
                        if (!string.IsNullOrWhiteSpace(guest.Name))
                        {
                            guests.Remove(guest);
                        }
                    }
                }//end of foreach loop

            }
            catch (Exception ex)
            {
                MessageBox.Show("btnLoadParticipantsList_Click\n" + ex.Message);
            }
}

        private void btnLoadPoll_Click(object sender, EventArgs e)
        {
            try
            {
                dlgOF.DefaultExt = "csv";
                dlgOF.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                if (dlgOF.ShowDialog() == DialogResult.OK)
                {
                    txtPollDate.Text = "Gathering Poll Data";
                    Application.DoEvents();
                    string dt = "";
                    poll = File.ReadAllText(dlgOF.FileName);
                    pollTakers.Clear();
                    
                    //read line by line
                    string[] spltPoll = poll.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    foreach (string line in spltPoll)
                    {
                        //separate by comma
                        string[] csv = line.Split(',');
                        
                        PollTaker p = new PollTaker();
                        
                        switch (csv.Length)
                        {
                            case 4:
                                if (csv[0].IndexOf(Properties.Settings.Default.PollDateFinder) > -1)
                                {
                                    dt = csv[2];
                                }
                                break;
                            case int n when n >= 6:
                                p.Username = csv[1];
                                p.Email = csv[2];
                                p.PollResult = csv[6];
                                
                                //add p to the polltakers hashset
                                pollTakers.Add(p);

                                break;

                        }//end switch
                    }

                    //add date
                    txtPollDate.Text = DateTime.Parse(dt).ToString("MM/dd/yyyy");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnLoadPoll_Click\n" + ex.Message);
            }
        }

        private void lstbxName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                lstbxEmail.SelectedIndex = lstbxName.SelectedIndex;
            }
            catch (Exception ex)
            {
                MessageBox.Show("lstbxName_SelectedIndexChanged\n" + ex.Message);
            }
        }

        private void lstbxEmail_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                lstbxName.SelectedIndex = lstbxEmail.SelectedIndex;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("lstbxEmail_SelectedIndexChanged\n" + ex.Message);
            }
        }

        private void lstbxName_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUN.Text))
                {
                    txtUN.Text = lstbxName.Text;
                }
                else
                {
                    txtUN.Text += "," + lstbxName.Text;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("lstbxName_MouseDoubleClick\n" + ex.Message);
            }
        }

        private void lstbxEmail_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtEM.Text))
                {
                    txtEM.Text = lstbxEmail.Text;
                }
                else
                {
                    txtEM.Text += "," + lstbxEmail.Text;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("lstbxEmail_MouseDoubleClick\n" + ex.Message);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void SaveToCSV(DataGridView DGV, string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    File.Delete(filename);
                }
                catch (IOException ex)
                {
                    MessageBox.Show("It wasn't possible to write the data to the disk." + ex.Message);
                }
            }
            int columnCount = DGV.ColumnCount;
            string columnNames = "";
            string[] output = new string[DGV.RowCount + 1];
            for (int i = 0; i < columnCount; i++)
            {
                columnNames += DGV.Columns[i].Name.ToString() + Properties.Settings.Default.CSVDelimiter;
            }
            output[0] += columnNames;
            for (int i = 1; (i - 1) < DGV.RowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    output[i] += DGV.Rows[i - 1].Cells[j].Value.ToString() + Properties.Settings.Default.CSVDelimiter;
                }
            }
            System.IO.File.WriteAllLines(filename, output, System.Text.Encoding.UTF8);
            
        }

        private void btnCompare_Click(object sender, EventArgs e)
        {
            try
            {
                //first check the dates and if they don't match, give a warning                
                if (txtPartDate.Text != txtPollDate.Text)
                {
                    if (MessageBox.Show("Date's don't match betwee Participants Report and Poll.\n " +
                        "Do you want to compare anywy?", "Compare Participants to Poll", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }

 
                //compare poll takers to participants to get the non poll takers
                for (int i = 0; i < lstbxName.Items.Count; i++)
                {
                    //add the non poll taker and if it's found that they did take the poll then remove them
                    string nonpolster = lstbxName.Items[i].ToString();
                    foreach (PollTaker pt in pollTakers)
                    {
                        if (pt.Email.IndexOf(lstbxEmail.Items[i].ToString()) > -1)
                        {
                            //it turns out they did take the poll so we can blank them out
                            nonpolster = "";
                        }
                        if (pt.Username.IndexOf(lstbxName.Items[i].ToString()) > -1)
                        {
                            //it turns out they did take the poll so we can blank them out
                            nonpolster = "";
                        }
                        
                    }

                    if (!string.IsNullOrWhiteSpace(nonpolster))
                    {
                        //this person didn't take the poll so add them to the list
                        nonpollers.Add(nonpolster);
                    }

                }
                //now that we have assembled our three categories: Poll Takers, Non-Poll Takers, Absentees
                //we can put them all together in a list called reports and then load that into
                //the datasourse of our gridview object. 

                int listsize = Math.Max(pollTakers.Count, Math.Max(nonpollers.Count, Math.Max(absentees.Count, guests.Count)));

                for (int i = 0; i < listsize; i++)
                {
                    reports.Add(new report());
                    
                }


                AddPollTakers();
                AddNonPollers();
                AddAbsentees();
                AddGuests();
                                
                dgvReport.DataSource = reports;

            }
            catch (Exception ex)
            {
                MessageBox.Show("btnCompare_Click\n" + ex.Message);
            }
        }

        private void AddPollTakers()
        {
            try
            {
                //add our first two values of date and total attendance
                //first get the attendance count
                int attnd = 1; //we start with 1 because we are including the host

                foreach (PollTaker p in pollTakers)
                {
                    try
                    {
                        //add the poll value to the attendance count
                        attnd += int.Parse(p.PollResult);
                    }
                    catch
                    {
                        //nothing to do here
                        continue;
                    }

                }

                string md = txtPartDate.Text;
                int i = 0;
                report firstr = new report();
                bool nodate = false;
                foreach (PollTaker p in pollTakers)
                {                    
                    if (attnd > 0)
                    {
                        firstr.Attendance = attnd.ToString();
                        firstr.Meeting_Date = md;
                        attnd = 0;
                        nodate = true;
                    }
                    else
                    {
                        
                        reports[i].Attendance = firstr.Attendance;
                        reports[i].Meeting_Date = firstr.Meeting_Date;
                        reports[i].Users_Who_Took_Poll = p.Username;
                        reports[i].Poll_Result = p.PollResult;
                        i++;
                        if (nodate)
                        {
                            firstr.Attendance = "";
                            firstr.Meeting_Date = "";
                            nodate = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddPollTakers\n" + ex.Message);
            }
        }

        private void AddNonPollers()
        {
            try
            {
                int i = 0;
                foreach (string np in nonpollers)
                {
                    reports[i].Failed_To_Take_Poll = np;
                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddNonPollers\n" + ex.Message);
            }
        }
        private void AddGuests()
        {
            try
            {
                Guest congguest;
                congguest.Name = Properties.Settings.Default.CongregationName;
                congguest.Email = Properties.Settings.Default.CongregationEmail;
                guests.Remove(congguest);
                int i = 0;
                foreach (Guest g in guests)
                {
                    reports[i].Guests = g.Name;
                    reports[i].G_Email = g.Email;
                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddGuests\n" + ex.Message);
            }
        }

        private void AddAbsentees()
        {
            try
            {
                int i = 0;
                foreach (Absentee a in absentees)
                {
                    //append missing data to the current record in reports
                    reports[i].M_M_UserName = a.Username;
                    reports[i].Missed_Meeting_MemberName = a.MemberName;
                    reports[i].Service_Group = a.GroupName;
                    reports[i].M_M_Email = a.Email;
                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddAbsentees\n" + ex.Message);
            }
        }

        

    }
}
