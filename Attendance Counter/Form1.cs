using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        string reportfn =  DateTime.Today.ToString("MM-dd-yyyy") + "_Report.csv";
        string guestName = "";
        string guestEmail = "";
        bool moveServiceGroup = false;
        TreeNode tnMoveServiceGroup;
        TreeNode tnMoveSGParent;


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
                        nnd.Nodes.Add("DefaultCount:" + txtDefaultCount.Text.Trim());
                    }
                    
                }
                if (!memberadded)
                {
                    //Group didnt exist so create it and add member
                    TreeNode nd = tvSG.Nodes.Add(txtServiceGroup.Text.Trim());
                    TreeNode nnd = nd.Nodes.Add("Member Name:" + txtMN.Text.Trim());
                    nnd.Nodes.Add("Emails:" + txtEM.Text.Trim());
                    nnd.Nodes.Add("Usernames:" + txtUN.Text.Trim());
                    nnd.Nodes.Add("DefaultCount:" + txtDefaultCount.Text.Trim());
                }

                //save tree
                SaveTree(tvSG, cfgPath);
                txtEM.Clear();
                txtMN.Clear();
                txtUN.Clear();
                txtDefaultCount.Text = "1";

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
                txtDefaultCount.Clear();
                if (moveServiceGroup)
                {
                    if (tvSG.SelectedNode.Level == 2)
                    {
                        tvSG.SelectedNode = tvSG.SelectedNode.Parent.Parent;
                    }
                    else if (tvSG.SelectedNode.Level == 1)
                    {
                        tvSG.SelectedNode = tvSG.SelectedNode.Parent;
                    }

                    string msg = string.Format("Are you sure you want to move {0} to {1}",tnMoveServiceGroup.Text, tvSG.SelectedNode.Text);
                    DialogResult mbb = MessageBox.Show(msg, "Move to new Service Group!", MessageBoxButtons.YesNoCancel);

                    switch (mbb)
                    {
                        case DialogResult.Yes:
                            tvSG.SelectedNode.Nodes.Add(tnMoveServiceGroup);
                            tnMoveServiceGroup = null;
                            tnMoveSGParent = null;
                            moveServiceGroup = false;
                            //save tree
                            SaveTree(tvSG, cfgPath);
                            break;
                        case DialogResult.No:
                            tvSG.SelectedNode = null;
                            break;
                        case DialogResult.Cancel:
                            tnMoveSGParent.Nodes.Add(tnMoveServiceGroup);
                            tnMoveServiceGroup = null;
                            tnMoveSGParent = null;
                            moveServiceGroup = false;
                            break;
                    }


                }
                if (tvSG.SelectedNode != null)
                {
                    if (tvSG.SelectedNode.Level == 0)
                    {
                        //node is root so it is a service group node
                        txtServiceGroup.Text = tvSG.SelectedNode.Text;
                        //nothing more to do here because we need to select the member name to
                        //get all the info
                    }
                    else if(tvSG.SelectedNode.Level == 1)
                    {
                        //this is the member name because it is the second tier node
                        txtServiceGroup.Text = tvSG.SelectedNode.Parent.Text;
                        txtMN.Text = tvSG.SelectedNode.Text.Replace("Member Name:","");
                        foreach(TreeNode node in tvSG.SelectedNode.Nodes)
                        {
                            if (node.Text.Contains("Emails:") )
                            {
                                txtEM.Text = node.Text.Replace("Emails:","");
                            }
                            else if (node.Text.Contains("Usernames:"))
                            {
                                txtUN.Text = node.Text.Replace("Usernames:", "");
                            }
                            else if (node.Text.Contains("DefaultCount:"))
                            {
                                txtDefaultCount.Text = node.Text.Replace("DefaultCount:", "");
                            }
                        }
                    }
                    else if (tvSG.SelectedNode.Level == 2)
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
                        //tvSG.SelectedNode.Nodes.Add("DefaultCount:" + txtDefaultCount.Text.Trim());
                        foreach (TreeNode node in tvSG.SelectedNode.Nodes)
                        {
                            if (node.Text.Contains("Emails:"))
                            {
                                tvSG.SelectedNode.Nodes[node.Index].Text = "Emails:" + txtEM.Text;
                            }
                            else if (node.Text.Contains("Usernames:"))
                            {
                                tvSG.SelectedNode.Nodes[node.Index].Text = "Usernames:" + txtUN.Text;
                            }
                            else if (node.Text.Contains("DefaultCount:"))
                            {
                                tvSG.SelectedNode.Nodes[node.Index].Text = "DefaultCount:" + txtDefaultCount.Text.Trim();
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
                        Guest gst = new Guest();

                        if (start)
                        {
                            if (csv.Length < 2)
                            {
                                continue;
                            }
                            if (!string.IsNullOrWhiteSpace(csv[0]))
                            {
                                lstbxName.Items.Add(csv[0]);
                                gst.Name = csv[0];
                            }
                            else
                            {
                                lstbxName.Items.Add("");
                            }
                            if (!string.IsNullOrWhiteSpace(csv[1]))
                            {
                                lstbxEmail.Items.Add(csv[1]);
                                gst.Email = csv[1];
                            }
                            else
                            {
                                lstbxEmail.Items.Add("");
                            }
                            //add the guest to the guests list
                            guests.Add(gst);

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
                                        
                                    }
                                }
                            }
                        }
                    }
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnLoadParticipantsList_Click\n" + ex.Message);
            }
}

        private void ParseServiceGroups()
        {
            try
            {
                //go thru each service group and find any matches to username and emails
                foreach (TreeNode gNode in tvSG.Nodes) //group name
                {

                    foreach (TreeNode mNode in gNode.Nodes)//member name
                    {
                        //add member to absentee hashset list and remove if found later
                        //a hashset is used because it only adds if it's unique
                        Absentee a = new Absentee(gNode.Text, mNode.Text);


                        foreach (TreeNode ueNode in mNode.Nodes)//username and email
                        {
                            //parse through all guests and eliminate any found
                            HashSet<Guest> gsts = new HashSet<Guest>(guests);
                            foreach (Guest g in gsts)
                            {
                                try
                                {
                                    if (ueNode.Text.ToUpper().IndexOf(g.Name.ToUpper()) > -1)
                                    {
                                        guests.Remove(g);
                                    }
                                    else if (!string.IsNullOrEmpty(g.Email))
                                    {
                                        if (ueNode.Text.ToUpper().IndexOf(g.Email.ToUpper()) > -1)
                                        {
                                            guests.Remove(g);
                                        }
                                    }
                                } 
                                catch
                                {
                                    continue;
                                }
                            }
                            //Usernames and Emails are seperated by commas
                            if (ueNode.Text.IndexOf("Usernames:") > -1)
                            {
                                a.Username = ueNode.Text;

                                foreach (string un in lstbxName.Items)
                                {
                                    if (string.IsNullOrWhiteSpace(un))
                                    {
                                        continue;
                                    }
                                    if (ueNode.Text.ToUpper().IndexOf(un.ToUpper()) > -1)
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

                                foreach (string em in lstbxEmail.Items)
                                {
                                    if (string.IsNullOrWhiteSpace(em))
                                    {
                                        continue;
                                    }
                                    if (ueNode.Text.ToUpper().IndexOf(em.ToUpper()) > -1)
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
                            if (a.Username.ToUpper() != txtHost.Text.ToUpper().Trim())
                            {
                                a.MemberName = a.MemberName.Replace("Member Name:", "");
                                a.Username = a.Username.Replace("Usernames:", "");
                                a.Email = a.Email.Replace("Emails:", "");
                                absentees.Add(a);
                            }
                        }

                    }
                }//end of foreach loop
            }
            catch (Exception ex)
            {
                MessageBox.Show("ParseServiceGroups\n" + ex.Message);
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
                    txtPollDate.Text = "Poll Loaded";
                    poll = File.ReadAllText(dlgOF.FileName);

                }
                  
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnLoadPoll_Click\n" + ex.Message);
            }
        }

        private void LoadPoll()
        {
            try
            {
                pollTakers.Clear();

                //read line by line
                string dt = "";
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
            catch (Exception ex)
            {
                MessageBox.Show("LoadPoll\n" + ex.Message);
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
            if (tabControl1.SelectedTab.Text == "Viewer" && !string.IsNullOrEmpty(Properties.Settings.Default.Folder))
            {
                lstbxFolder.Items.Clear();
                DirectoryInfo dinfo = new DirectoryInfo(Properties.Settings.Default.Folder);
                FileInfo[] Files = dinfo.GetFiles("*.csv");
                foreach (FileInfo file in Files)
                {
                    lstbxFolder.Items.Add(file.Name);
                }
            }

        }

        private void SaveToCSV(DataGridView DGV, string filename)
        {
            string dlmtr = Properties.Settings.Default.CSVDelimiter;
            string drepl = Properties.Settings.Default.DelimterReplacement;
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
                columnNames += DGV.Columns[i].Name.ToString().Replace("_", " ") + Properties.Settings.Default.CSVDelimiter;
            }
            output[0] += columnNames;
            for (int i = 1; (i - 1) < DGV.RowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    if (DGV.Rows[i - 1].Cells[j].Value == null)
                    {
                        output[i] += dlmtr;
                    }
                    else
                    {

                        output[i] += DGV.Rows[i - 1].Cells[j].Value.ToString().Replace(dlmtr, drepl) + dlmtr;
                    }
                }
            }
            System.IO.File.WriteAllLines(filename, output, System.Text.Encoding.UTF8);
            
        }

        private void btnCompare_Click(object sender, EventArgs e)
        {
            try
            {
                //clear all vars
                dgvReport.DataSource = null;
                dgvReport.Refresh();
                absentees.Clear();
                nonpollers.Clear();

                GetKHConf();
                //add host to the poll count
                if (!poll.EndsWith(Environment.NewLine))
                {
                    poll += Environment.NewLine;
                }
                //#,User Name,User Email,Submitted Date/Time,poll message, poll
                poll += "0, Host, , , , ,1" + Environment.NewLine;

                ParseServiceGroups();
                LoadPoll();
                

                //first check the dates and if they don't match, give a warning                
                if (txtPartDate.Text != txtPollDate.Text)
                {
                    if (MessageBox.Show("Date's don't match betwee Participants Report and Poll.\n " +
                        "Do you want to compare anywy?", "Compare Participants to Poll", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }

                
                dgvReport.DataSource = "";
 
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

                reports[0].Meeting_Date = txtPartDate.Text;
                reports[1].Meeting_Date = "Non-Poll Takers:";
                reports[2].Meeting_Date = "Total:";

                reports[0].Attendance = string.Format("=SUM(D2:D{0})", reports.Count + 5);
                reports[1].Attendance = string.Format("=SUM(F2:F{0})", nonpollers.Count + 5);
                reports[2].Attendance = "=SUM(B2:B3)";


                dgvReport.DataSource = reports;

                int attnd = 0;
                int nonpoll = 0;
                foreach (DataGridViewRow row in dgvReport.Rows)
                {
                    if (row.Cells["Poll_Result"].Value != null)
                    {
                        try
                        {
                            attnd += int.Parse(row.Cells["Poll_Result"].Value.ToString());
                        }
                        catch { }
                    }
                    if (row.Cells["Default_Count"].Value != null)
                    {
                        try
                        {
                            nonpoll += int.Parse(row.Cells["Default_Count"].Value.ToString());
                        }
                        catch { }
                    }
                }

                txtAttendance.Text = attnd.ToString();
                txtNonPolledTotal.Text = nonpoll.ToString();

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

                int i = 0;

                foreach (PollTaker p in pollTakers)
                {

                    reports[i].Users_Who_Took_Poll = p.Username;
                    reports[i].Poll_Result = p.PollResult;
                    i++;

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
                    if (np == Properties.Settings.Default.CongregationName) { continue; }
                    reports[i].Failed_To_Take_Poll = np;
                    bool dnotfound = true;
                    foreach (TreeNode grp in tvSG.Nodes)
                    {
                        foreach (TreeNode mn in grp.Nodes)
                        {
                            if (mn.Nodes[1].Text.ToLower().Contains(np.ToLower()))
                            {
                                dnotfound = false;
                                try
                                {
                                    reports[i].Default_Count = mn.Nodes[2].Text.Replace("DefaultCount:","");
                                }
                                catch
                                {
                                    reports[i].Default_Count = "Undefined";
                                }
                                break;
                            }
                        }
                        if (!dnotfound)
                        {
                            break;
                        }
                    }
                    if (dnotfound)
                    {
                        reports[i].Default_Count = "Undefined";

                    }
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

        private void txtHost_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tvSG.SelectedNode.Text))
                {
                    foreach (TreeNode tn in tvSG.SelectedNode.Nodes)
                    {
                        if (tn.Text.IndexOf("Usernames:") > -1)
                        {
                            txtHost.Text = tn.Text.Replace("Usernames:", "");
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("txtHost_MouseDoubleClick\n" + ex.Message);
            }
        }

        

        private void txtMN_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtMN_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtMN.Text)){
                    txtMN.Text = guestName;
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("txtMN_MouseDoubleClick\n" + ex.Message);
            }
        }

        private void txtUN_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUN.Text)) {
                    txtUN.Text = guestName;
                }
                else
                {
                    txtUN.Text += "," + guestName;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("txtUN_MouseDoubleClick\n" + ex.Message);
            }
        }

        private void txtEM_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtEM.Text)) {
                    txtEM.Text = guestEmail;
                }
                else
                {
                    txtEM.Text += "," + guestEmail;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("txtEM_MouseDoubleClick\n" + ex.Message);
            }
        }

        private void dgvReport_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvReport.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
                {
                    guestName = "";
                    guestEmail = "";
                    if (e.ColumnIndex == 10)
                    {
                        //guest name is selected
                        guestName = dgvReport.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                        if (dgvReport.Rows[e.RowIndex].Cells[11].Value != null)
                        {
                            guestEmail = dgvReport.Rows[e.RowIndex].Cells[11].Value.ToString();
                        }
                    }
                    else if (e.ColumnIndex == 11)
                    {
                        //Gest email is selected
                        guestEmail = dgvReport.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                        if (dgvReport.Rows[e.RowIndex].Cells[10].Value != null)
                        {
                            guestName = dgvReport.Rows[e.RowIndex].Cells[10].Value.ToString();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("dgvReport_CellClick\n" + ex.Message);
            }
        }

        private TreeNode FindFirstNode(string search, TreeView treeView)
        {
            try
            {
                TreeNode Result = new TreeNode();
                foreach (TreeNode t in treeView.Nodes)
                {
                    if (t.Text.IndexOf(search) > -1)
                    {
                        Result = t;
                        break;
                    }
                }
                if (Result != null) //couldn't find string in parent, now try children
                {
                    foreach (TreeNode t in treeView.Nodes)
                    {
                        Result = FindChildNode(search, t);
                        if (Result != null)
                        {
                            break;
                        }
                    }
                }
                return Result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindFirstNode\n" + ex.Message);
                return null;
            }
        }

        private TreeNode FindChildNode(string search, TreeNode treeNode)
        {
            try
            {
                TreeNode Result = new TreeNode();
                foreach (TreeNode t in treeNode.Nodes)
                {
                    if (t.Text.IndexOf(search) > -1)
                    {
                        Result = t;
                        break;
                    }
                }
                if (Result != null) //couldn't find string in parent, now try children
                {
                    foreach (TreeNode t in treeNode.Nodes)
                    {
                        Result = FindChildNode(search, t);
                        if (Result != null)
                        {
                            break;
                        }

                    }
                }
                return Result;

            }
            catch (Exception ex)
            {
                MessageBox.Show("FindChildNode\n" + ex.Message);
                return null;
            }
        }

        private void btnSearchGroups_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtSearchGroups.Text))
                {
                    tvSG.Focus();
                    foreach (TreeNode Grp in tvSG.Nodes) //group name
                    {
                        if (Grp.Text.ToLower().Contains(txtSearchGroups.Text.ToLower().Trim()))
                        {
                            tvSG.SelectedNode = Grp;
                            break;
                        }
                        foreach (TreeNode MN in Grp.Nodes)
                        {
                            if (MN.Text.ToLower().Contains(txtSearchGroups.Text.ToLower().Trim()))
                            {
                                tvSG.SelectedNode = MN;
                                break;
                            }
                            foreach (TreeNode EU in MN.Nodes)
                            {
                                if (EU.Text.ToLower().Contains(txtSearchGroups.Text.ToLower().Trim()))
                                {
                                    tvSG.SelectedNode = EU;
                                    break;
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnSearchGroups_Click\n" + ex.Message);
            }
        }

        private void GetKHConf()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtKH.Text)) { return; }

                //add listing to poll that seperates KHConf from Zoom
                if (!poll.EndsWith(Environment.NewLine))
                {
                    poll += Environment.NewLine;
                }
                //#,User Name,User Email,Submitted Date/Time,poll message, poll
                poll += "0,!!KHConf BELOW!!, , , , , VVVV" + Environment.NewLine;



                //parse line by line

                string[] khln = txtKH.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int i = 0;
                string name = "";
                string count = "";
                foreach (string ln in khln)
                {                    
                    //deterimine if phone number or Name or time or count
                    switch (i)
                    {
                        case 0:
                            name = ln.Trim();
                            break;
                        case 1:
                            if (!ln.Contains("->")) //it's not time so must be name
                            {
                                name = ln.Trim();
                            }
                            break;
                        case 2:
                            break;
                        case 3:
                            try
                            {
                                if (int.Parse(ln) > 0)
                                {
                                    count = ln.Trim();
                                    i = 0;
                                }
                            }
                            catch 
                            {

                            }
                            break;
                        case 4:
                            count = ln.Trim();
                            i = 0;
                            break;
                        default:
                            i = 0;
                            break;
                    }
                    i++;
                    if (!string.IsNullOrEmpty(count))
                    {
                        //check if name is already in particpants list if so ignore
                        bool nameisnew = true;
                        foreach(string n in lstbxName.Items)
                        {
                            if (n.ToLower().Contains(name.ToLower()))
                            {
                                nameisnew = false;
                            }
                        }
                        if (nameisnew)
                        {
                            //add name and count to participants and poll
                            lstbxName.Items.Add(name);
                            lstbxEmail.Items.Add("");
                            if (!poll.EndsWith(Environment.NewLine))
                            {
                                poll += Environment.NewLine;
                            }
                            //#,User Name,User Email,Submitted Date/Time,poll message, poll
                            poll += "0," + name + ", e, d, m, " + ", " + count + Environment.NewLine;                            
                        }
                        i = 0;
                        count = "";
                    }
                    
                } //end foreach


            }
            catch (Exception ex)
            {
                MessageBox.Show("KHConf\n" + ex.Message);
            }
        }

        private void btnSaveCSV_Click(object sender, EventArgs e)
        {
            try
            {                
                dlgSCSV.FileName = reportfn;
                if (dlgSCSV.ShowDialog() == DialogResult.OK)
                {
                    SaveToCSV(dgvReport, dlgSCSV.FileName);
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnSaveCSV_Click\n" + ex.Message);
            }
        }

        private void txtSearchGroups_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                btnSearchGroups.PerformClick();
            }
        }

        private void txtDefaultCount_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void dgvReport_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int attnd = 0;
                int nonpoll = 0;
                foreach (DataGridViewRow row in dgvReport.Rows)
                {
                    if (row.Cells["Poll_Result"].Value != null)
                    {
                        try
                        {
                            attnd += int.Parse(row.Cells["Poll_Result"].Value.ToString());
                        }
                        catch { }
                    }
                    if (row.Cells["Default_Count"].Value != null)
                    {
                        try
                        {
                            nonpoll += int.Parse(row.Cells["Default_Count"].Value.ToString());
                        }
                        catch { }
                    }
                }

                txtAttendance.Text = attnd.ToString();
                txtNonPolledTotal.Text = nonpoll.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("dgvReport_CellEndEdit\n" + ex.Message);
            }
        }

        private void btnMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (tvSG.SelectedNode != null)
                {
                    if (tvSG.SelectedNode.Level > 0)
                    {
                        string msg = "Are you sure you want to move:" + tvSG.SelectedNode.Text + " To another Service Group?";
                        if (MessageBox.Show(msg, "Move to new Service Group!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            tnMoveServiceGroup = tvSG.SelectedNode;
                            tnMoveSGParent = tvSG.SelectedNode.Parent;
                            tvSG.SelectedNode.Remove();
                            tvSG.CollapseAll();
                            msg = "Select a service group to add to:";
                            MessageBox.Show(msg, "Move to new Service Group!");
                            moveServiceGroup = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnMove_Click\n" + ex.Message);
            }
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (dlgFB.ShowDialog() == DialogResult.OK)
                {
                    lstbxFolder.Items.Clear();
                    DirectoryInfo dinfo = new DirectoryInfo(dlgFB.SelectedPath);
                    FileInfo[] Files = dinfo.GetFiles("*.csv");
                    foreach (FileInfo file in Files)
                    {
                        Properties.Settings.Default.Folder = dlgFB.SelectedPath;
                        Properties.Settings.Default.Save();
                        lstbxFolder.Items.Add(file.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnSelectFolder_Click\n" + ex.Message);
            }
        }

        public DataTable readCSV(string filePath)
        {
            var dt = new DataTable();
            // Creating the columns
            File.ReadLines(filePath).Take(1)
                .SelectMany(x => x.Split(new[] { ',' }, StringSplitOptions.None))
                .ToList()
                .ForEach(x => dt.Columns.Add(x.Trim()));

            // Adding the rows
            File.ReadLines(filePath).Skip(1)
                .Select(x => x.Split(','))
                .ToList()
                .ForEach(line => dt.Rows.Add(line));
            return dt;
        }

        private void lstbxFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                dgViewer.DataSource = null;
                dgViewer.Refresh();
                dgViewer.DataSource = readCSV(Properties.Settings.Default.Folder + @"\" + lstbxFolder.SelectedItem.ToString());
                int attnd = 0;
                int nonpoll = 0;
                foreach (DataGridViewRow row in dgViewer.Rows)
                {
                    if (row.Cells[3].Value != null)
                    {
                        try
                        {
                            attnd += int.Parse(row.Cells[3].Value.ToString());
                        }
                        catch { }
                    }
                    if (row.Cells[5].Value != null)
                    {
                        try
                        {
                            nonpoll += int.Parse(row.Cells[5].Value.ToString());
                        }
                        catch { }
                    }
                }

                txtVAttend.Text = attnd.ToString();
                txtVNonPolled.Text = nonpoll.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("lstbxFolder_SelectedIndexChanged\n" + ex.Message);
            }
        }

        private void txtVAttend_TextChanged(object sender, EventArgs e)
        {
            try
            {
                txtVTotal.Text = (int.Parse(txtVAttend.Text) + int.Parse(txtVNonPolled.Text)).ToString();
            }
            catch
            {
                txtVTotal.Text = "";
            }
        }

        private void txtVNonPolled_TextChanged(object sender, EventArgs e)
        {
            try
            {
                txtVTotal.Text = (int.Parse(txtVAttend.Text) + int.Parse(txtVNonPolled.Text)).ToString();
            }
            catch 
            {
                txtVTotal.Text = "";            
            }
        }

        private void txtAttendance_TextChanged(object sender, EventArgs e)
        {
            try
            {
                txtATotal.Text = (int.Parse(txtAttendance.Text) + int.Parse(txtNonPolledTotal.Text)).ToString();
            }
            catch
            {
                txtATotal.Text = "";
            }
        }

        private void txtNonPolledTotal_TextChanged(object sender, EventArgs e)
        {
            try
            {
                txtATotal.Text = (int.Parse(txtAttendance.Text) + int.Parse(txtNonPolledTotal.Text)).ToString();
            }
            catch
            {
                txtATotal.Text = "";
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                
                
                //Process.Start(Application.StartupPath + @"\tutvid.mp4");

            }
            catch (Exception ex)
            {
                MessageBox.Show("btnPlay_Click\n" + ex.Message);
            }
        }
    }
}
