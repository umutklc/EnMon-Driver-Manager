﻿using EnMon_Driver_Manager.DataBase;
using EnMon_Driver_Manager.Forms;
using EnMon_Driver_Manager.Models;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

//using System.Windows.Controls;
using System.Windows.Forms;

namespace EnMon_Driver_Manager
{
    public partial class frm_Email : Form

    {
        #region Private Properties

        private DataTable dt_MailGroups { get; set; }

        private AbstractDBHelper DBHelper_EmailSettings;

        #endregion Private Properties

        #region Public Properties

        public string EMailAddressForTestMail;

        public EmailSettingsEventHandler MailClientSettingsUpdateRequested;

        #endregion Public Properties

        #region Constructors

        public frm_Email()
        {
            InitializeComponent();
        }

        public frm_Email(AbstractDBHelper dbhelper)
        {
            InitializeComponent();
            DBHelper_EmailSettings = dbhelper;
        }

        #endregion Constructors

        #region Events

        private void btn_Update_Click(object sender, EventArgs e)
        {
            try
            {
                //OnMailClientSettingsUpdateRequested(txt_MailServerName.Text, txt_MailServerPort.Text, txt_UserName.Text, txt_Password.Text);
                if (UpdateMailGroups() & AddMailGroupsToComboBox())
                {
                    MessageBox.Show("Ayarlar başarılı bir şekilde güncellendi", Constants.MessageBoxHeader, MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ayarlar güncellenemedi. Ayrıntılı bilgi için log dosyasına bakınız", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Instance.Error("E_Posta ayarları güncellenemedi => {0}", ex.Message);
            }
        }

        private void btn_NewGroup_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.Items.Count == 0)
                {
                    AddMailGroupsToComboBox();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database'den e-posta grup bilgileri çekilirken hata oluştu", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Instance.Error("Database'den mail grup bilgileri çekilirken hata oluştu => {0} ", ex.Message);
            }
        }

        private async void frm_Email_Load(object sender, EventArgs e)
        {
            dt_MailGroups = new DataTable();
        }

        private void btn_AddSelectedUserToGroup_Click(object sender, EventArgs e)
        {
            if (lstBox_UsersNotAddedToGroup.SelectedItem != null)
            {
                User user = (User)lstBox_UsersNotAddedToGroup.SelectedItem;
                MoveUserToGroupListBox(user);
            }
        }

        private void btn_AddAllUsersToGroup_Click(object sender, EventArgs e)
        {
            if (lstBox_UsersNotAddedToGroup.Items.Count > 0)
            {
                List<User> _users = lstBox_UsersNotAddedToGroup.Items.Cast<User>().ToList();
                foreach (User u in _users)
                {
                    MoveUserToGroupListBox(u);
                }
            }
        }

        private void btn_RemoveSelectedUserFromGroup_Click(object sender, EventArgs e)
        {
            if (lstBox_UsersAddedToGroup.SelectedItem != null)
            {
                RemoveUserFromGroupListBox((User)lstBox_UsersAddedToGroup.SelectedItem);
            }
        }

        private void btn_RemoveAllUsersFromGroup_Click(object sender, EventArgs e)
        {
            if (lstBox_UsersAddedToGroup.Items.Count > 0)
            {
                List<User> _users = lstBox_UsersAddedToGroup.Items.Cast<User>().ToList();
                foreach (User u in _users)
                {
                    RemoveUserFromGroupListBox(u);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbBox = (ComboBox)sender;
            try
            {
                ShowGroupMailAddresses((MailGroup)cbBox.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database'den e-posta bilgileri çekilirken hata oluştu", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Instance.Error("Database'den e-posta bilgileri çekilirken hata oluştu => {0} ", ex.Message);
            }
        }

        private void btn_DeleteGroup_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                DialogResult result = MessageBox.Show("Bu mail grubu ve bu mail grubuna ait tüm e-posta alarm gönderimleri silininecektir. \nDevam etmek istiyor musunuz?", Constants.MessageBoxHeader, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    MailGroup mg = ((MailGroup)comboBox1.SelectedItem);
                    DBHelper_EmailSettings.DeleteMailGroup(mg);
                    AddMailGroupsToComboBox();
                    comboBox1.SelectedItem = null;
                }
            }
        }

        private void btn_NewGroup_Click_1(object sender, EventArgs e)
        {
            Load_frm_AddNewMailAlarm();
        }

        private void Frm_addNewMailGroup_addEmailGroup(object source, frm_AddNewMailGroupArgs args)
        {
            if (DBHelper_EmailSettings.AddNewMailGroup(args.mailgroup))
            {
                MessageBox.Show("E-Posta Grubu Oluşturuldu", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Information);
                AddMailGroupsToComboBox();
            }
            else
            {
                MessageBox.Show("E-Posta grubu oluştururken hata oluştu.\nAyrıntılı bilgi için log dosyasına bakınız.", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Events



        #region Private Methods

        private void btn_ChangeGroupName_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                using (frm_ChangeGroupName frm_changeGroupName = new frm_ChangeGroupName((MailGroup)comboBox1.SelectedItem))
                {
                    frm_changeGroupName.ClickedChangeGroupNameButton += frm_changeGroupName_ClickedChangeGroupName;
                    DialogResult result = frm_changeGroupName.ShowDialog();
                }
            }
        }

        private void RefreshMailGroupBox()
        {
            comboBox1.SelectedItem = null;
            lstBox_UsersAddedToGroup.Items.Clear();
            lstBox_UsersNotAddedToGroup.Items.Clear();
            comboBox1.Items.Clear();
            AddMailGroupsToComboBox();
        }

        private void OnMailClientSettingsUpdateRequested(string _MailServerName, string _MailServerPort, string _UserName, string _Password)
        {
            EmailSettingsEventArgs args = new EmailSettingsEventArgs();

            args.MailServerName = _MailServerName;
            args.MailServerPort = _MailServerPort;
            args.Username = _UserName;
            args.Password = _Password;

            MailClientSettingsUpdateRequested?.Invoke(this, args);
        }

        private bool AddMailGroupsToComboBox()
        {
            List<MailGroup> mailGroups;
            try
            {
                mailGroups = DBHelper_EmailSettings.GetMailGroups();
                if (mailGroups.Count > 1)
                {
                    // sonradan comboboxa tekrardan atayabilmek için seçili mail grupalınıyor
                    MailGroup _mailGroup = new MailGroup();
                    if (comboBox1.SelectedItem != null)
                    {
                        _mailGroup = (MailGroup)comboBox1.SelectedItem;
                    }

                    comboBox1.Items.Clear();

                    // Mailgroup
                    mailGroups.Remove(mailGroups.Where((mg) => mg.Name == "No Group").First());
                    foreach (MailGroup mg in mailGroups)
                    {
                        comboBox1.Items.Add(mg);
                    }

                    // Mail grupları veritabanından çekilmeden önce eğer combobox'da seçili mailgroup varsa combobox'ın mevcut itemları silinip tekrardan atandığı için
                    // seçili mailgroup tekrardan comboboxun selecteditem property sine atanıyor
                    if (_mailGroup != null)
                    {
                        if (mailGroups.FindIndex((m) => m.Name == _mailGroup.Name) >= 0)
                        {
                            foreach (var cbi in comboBox1.Items)
                            {
                                if ((cbi as MailGroup).Name == _mailGroup.Name)
                                {
                                    comboBox1.SelectedItem = cbi;
                                    break;
                                }
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    Log.Instance.Info("Database'de kayıtlı mail grubu bulunamadı.");
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ShowGroupMailAddresses(MailGroup _mailGroup)
        {
            lstBox_UsersAddedToGroup.Items.Clear();
            lstBox_UsersNotAddedToGroup.Items.Clear();
            List<User> users = DBHelper_EmailSettings.GetAllUsersMailInfo();
            if (_mailGroup != null)
            {
                List<User> _usersInGroup = GetUsersForSelectedGroup(_mailGroup.ID);
                if (_usersInGroup != null)
                {
                    foreach (User u in _usersInGroup)
                    {
                        lstBox_UsersAddedToGroup.Items.Add(u);
                        users.RemoveAll((i) => i.Email == u.Email);
                    }

                    foreach (User u in users)
                    {
                        lstBox_UsersNotAddedToGroup.Items.Add(u);
                    }
                }
            }
        }

        private List<User> GetUsersForSelectedGroup(uint _groupID)
        {
            List<User> users = DBHelper_EmailSettings.GetMailRecipients(_groupID);

            return users;
        }

        private void MoveUserToGroupListBox(User _user)
        {
            lstBox_UsersNotAddedToGroup.Items.Remove(_user);
            lstBox_UsersAddedToGroup.Items.Add(_user);
        }

        private void RemoveUserFromGroupListBox(User _user)
        {
            lstBox_UsersNotAddedToGroup.Items.Add(_user);
            lstBox_UsersAddedToGroup.Items.Remove(_user);
        }

        private void Load_frm_AddNewMailAlarm()
        {
            uint ID = DBHelper_EmailSettings.GetNextMailGroupID();
            frm_AddNewMailGroup frm_addNewMailGroup = new frm_AddNewMailGroup(ID);
            frm_addNewMailGroup.addEmailGroup += Frm_addNewMailGroup_addEmailGroup;
            frm_addNewMailGroup.ShowDialog();
        }

        private bool UpdateMailGroups()
        {
            try
            {
                MailGroup mg = (MailGroup)comboBox1.SelectedItem;
                foreach (var item in lstBox_UsersNotAddedToGroup.Items)
                {
                    DBHelper_EmailSettings.UpdateUserMailGroup(((User)item).ID, 1);
                }
                foreach (var item in lstBox_UsersAddedToGroup.Items)
                {
                    DBHelper_EmailSettings.UpdateUserMailGroup(((User)item).ID, mg.ID);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Instance.Error("{0}: E-posta kullanıcı grupları güncellenirken beklenmedik hata oluştu => {1}", this.GetType().Name, ex.Message);
                return false;
            }
        }

        private bool UpdateMailClientSettings(string _mailServerName, string _mailServerPort, string _userName, string _password, string _fromMailAddress, bool _enableSSL)
        {
            try
            {
                if (File.Exists(Constants.MailClientConfigFileLocation))
                {
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(Constants.MailClientConfigFileLocation);

                    data["MailClient Parameters"]["MailServerName"] = _mailServerName;
                    data["MailClient Parameters"]["MailServerPort"] = _mailServerPort;
                    data["MailClient Parameters"]["UserName"] = _userName;
                    data["MailClient Parameters"]["Password"] = _password;
                    data["MailClient Parameters"]["MailAddress"] = _fromMailAddress;
                    data["MailClient Parameters"]["EnableSLL"] = _enableSSL.ToString().ToUpper();
                    parser.WriteFile(Constants.MailClientConfigFileLocation, data);

                    return true;
                }
                else
                {
                    MessageBox.Show("E-Posta Konfigurasyon dosyası bulunamadı", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Log.Instance.Warn("E-Posta ayarlarını guncellerken {0} dosya konumunda -eposta konfigurasyon dosyası bulunamadı", Constants.MailClientConfigFileLocation);
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void frm_changeGroupName_ClickedChangeGroupName(object source, ChangeGroupNameEventArgs e)
        {
            try
            {
                string newName = e.Name;
                if (DBHelper_EmailSettings.UpdateGroupName((MailGroup)comboBox1.SelectedItem, newName))
                {
                    MessageBox.Show("Grup Adı değiştirildi", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Log.Instance.Info("{0} mail grubunun adı {1} olarak değiştirildi", ((MailGroup)comboBox1.SelectedItem).Name, e.Name);
                    RefreshMailGroupBox();
                }
                else
                {
                    MessageBox.Show("Grup Adı değiştirilemedi", Constants.MessageBoxHeader, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error("Grup Adı değiştirilemedi: => {0}", ex.Message);
            }
        }

        #endregion Private Methods

        private void grp_ServerSettings_Enter(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void kryptonGroupBox1_Paint(object sender, PaintEventArgs e)
        {

        }
    }

    public class EmailSettingsEventArgs : EventArgs
    {
        public string MailServerName { get; set; }

        public string MailServerPort { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    public delegate void EmailSettingsEventHandler(object source, EmailSettingsEventArgs args);
}