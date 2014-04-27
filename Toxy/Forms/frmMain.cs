﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

using SharpTox;

namespace Toxy
{
    public partial class frmMain : Form
    {
        private Tox tox;
        private string id;
        private int currfriendnum = -1;

        private Dictionary<int, List<string>> messagedic = new Dictionary<int,List<string>>();

        public frmMain()
        {
            InitializeComponent();

            tox = new Tox(false);
            tox.OnFriendRequest += OnFriendRequest;
            tox.OnFriendMessage += OnFriendMessage;
            tox.OnFriendAction += OnFriendAction;
            tox.OnConnectionStatusChanged += OnConnectionStatusChanged;
            tox.OnNameChange += OnNameChange;

            if (File.Exists("data"))
            {
                if (!tox.Load("data"))
                {
                    MessageBox.Show("Could not load tox data, this program will now exit.");
                    Close();
                }
            }
            
            bool bootstrap_success = false;
            foreach(ToxNode node in Nodes)
            {
                if (tox.TryBootstrap(node))
                {
                    bootstrap_success = true;
                    break;
                }
            }

            if (!bootstrap_success)
            {
                MessageBox.Show("Could not bootstrap from any of the addresses");
                Close();
            }

            tox.Start();

            foreach(int friendnumber in tox.GetFriendlist())
            {
                messagedic.Add(friendnumber, new List<string>());

                ListViewItem item = new ListViewItem(tox.GetName(friendnumber));
                item.SubItems.Add("Offline");
                listFriends.Items.Add(item);
            }

            id = tox.GetAddress();
        }

        private void OnNameChange(int friendnumber, string newname)
        {
            BeginInvoke(((Action)(() => 
            {
                if ((listFriends.Items.Count - 1) >= friendnumber)
                {
                    listFriends.BeginUpdate();
                    listFriends.Items[friendnumber].SubItems[0].Text = newname;
                    listFriends.EndUpdate();
                }
                else
                {
                    //we're missing someone in our list!
                }
            })));
        }

        private void OnConnectionStatusChanged(int friendnumber, byte status)
        {
            BeginInvoke(((Action)(() =>
                {
                    if (status == 0)
                    {
                        //went offline
                        if ((listFriends.Items.Count - 1) >= friendnumber)
                        {
                            listFriends.BeginUpdate();
                            listFriends.Items[friendnumber].SubItems[1].Text = "Offline";
                            listFriends.EndUpdate();
                        }
                        else
                        {
                            //looks like we're missing someone in our list!
                        }
                    }
                    else
                    {
                        //online
                        if ((listFriends.Items.Count - 1) >= friendnumber)
                        {
                            listFriends.BeginUpdate();
                            listFriends.Items[friendnumber].SubItems[1].Text = "Online";
                            listFriends.EndUpdate();
                        }
                        else
                        {
                            //looks like we're missing someone in our list!
                        }
                    }
                })));
        }

        private void OnFriendAction(int friendnumber, string action)
        {
            string line = " * " + tox.GetName(friendnumber) + " " + action;
            messagedic[friendnumber].Add(line);

            if (currfriendnum == friendnumber)
            {
                BeginInvoke(((Action)(() =>
                    {
                        txtConversation.AppendText(line);
                        txtConversation.AppendText(Environment.NewLine);
                    })));
            }
        }

        private void OnFriendMessage(int friendnumber, string message)
        {
            string line = "<" + tox.GetName(friendnumber) + "> " + message;
            messagedic[friendnumber].Add(line);

            if (currfriendnum == friendnumber)
            {
                BeginInvoke(((Action)(() =>
                    {
                        txtConversation.AppendText(line);
                        txtConversation.AppendText(Environment.NewLine);
                    })));
            }
        }

        private void OnFriendRequest(string id, string message)
        {
            frmFriendRequest form = new frmFriendRequest(id, message);
            DialogResult result = form.ShowDialog();

            if (result == DialogResult.Yes)
            {
                int friendnumber = tox.AddFriendNoRequest(id);

                ListViewItem item = new ListViewItem(id);
                item.SubItems.Add("Offline");

                messagedic.Add(friendnumber, new List<string>());

                //should appear at the correct index/id automatically
                BeginInvoke(((Action)(() => listFriends.Items.Add(item))));
            }
        }

        private void Main_Load(object sender, EventArgs e) 
        {
            if (listFriends.Items.Count == 0)
                return;

            listFriends.Items[0].Selected = true;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            tox.Save("data");
            tox.Kill();
        }

        private void btnViewID_Click(object sender, EventArgs e)
        {
            frmID form = new frmID(id);
            form.ShowDialog();
        }

        private void btnAddFriend_Click(object sender, EventArgs e)
        {
            frmAddFriend form = new frmAddFriend();
            form.ShowDialog();

            if (form.ID != null)
            {
                int friendnumber = tox.AddFriend(form.ID, "Hey, I would like to add you as a friend!");
                messagedic.Add(friendnumber, new List<string>());

                ListViewItem item = new ListViewItem(form.ID);
                item.SubItems.Add("Offline");
                listFriends.Items.Add(item);
            }
        }

        private static ToxNode[] Nodes = new ToxNode[] {
            new ToxNode("192.184.81.118", 33445, "5CD7EB176C19A2FD840406CD56177BB8E75587BB366F7BB3004B19E3EDC04143", false),
            new ToxNode("107.161.21.13", 33445, "5848E6344856921AAF28DAB860C5816780FE0C8873AAC415C1B7FA7FAA4EF046", false),
            new ToxNode("37.187.46.132", 33445, "C021232F9AC83914A45DFCF242129B216FED5ED34683F385D932A66BC9178270", false),
        };

        private void txtToSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != Convert.ToChar(Keys.Return))
                return;

            if (currfriendnum != -1)
            {
                if (tox.GetFriendConnectionStatus(currfriendnum) != 1)
                    return;

                if (txtToSend.Text.StartsWith("/me "))
                {
                    string action = txtToSend.Text.Substring(4, txtToSend.Text.Length - 1);
                    tox.SendAction(currfriendnum, action);

                    string line = " * " + tox.GetName(currfriendnum) + " " + action;
                    messagedic[currfriendnum].Add(line);

                    txtConversation.AppendText(line);
                    txtConversation.AppendText(Environment.NewLine);
                    txtToSend.Text = "";

                    e.Handled = true;
                }
                else
                {
                    tox.SendMessage(currfriendnum, txtToSend.Text);

                    string line = "<" + tox.GetSelfName() + "> " + txtToSend.Text;
                    messagedic[currfriendnum].Add(line);

                    txtConversation.AppendText(line);
                    txtConversation.AppendText(Environment.NewLine);
                    txtToSend.Text = "";

                    e.Handled = true;
                }
            }
        }

        private void listFriends_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView view = (ListView)sender;

            if (view.SelectedItems.Count == 0)
                return;

            int friendnumber = view.SelectedItems[0].Index;
            currfriendnum = friendnumber;

            if (friendnumber == currfriendnum)
                return;

            txtConversation.Text = "";

            if (messagedic.ContainsKey(friendnumber))
            {
                foreach(string line in messagedic[friendnumber])
                {
                    txtConversation.AppendText(line);
                    txtConversation.AppendText(Environment.NewLine);
                }
            }
            else
            {
                messagedic.Add(friendnumber, new List<string>());
            }
        }
    }
}
