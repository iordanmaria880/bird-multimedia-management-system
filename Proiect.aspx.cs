using NAudio.Wave;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace ProiectBDM
{
    public partial class Proiect : System.Web.UI.Page
    {
        OracleConnection con;

        protected void Page_Load(object sender, EventArgs e)
        {
            string cons = "User ID=STUD_IORDANM; Password=student; Data Source=(DESCRIPTION=" +
                "(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=37.120.249.41)(PORT=1521)))" +
                "(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=orcls)));";
            con = new OracleConnection(cons);
        }

        //GESTIONAREA RESURSELOR DE TIP IMAGINE

        // INSERARE IMAGINE
        protected void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                // Verifica daca s-a selectat un fisier
                if (!fuImagine.HasFile)
                {
                    lblMsgAdd.Text = "Selectează o imagine pentru încărcare.";
                    return;
                }

                // Preia imaginea in memorie sub forma de byte[]
                byte[] imgBytes = fuImagine.FileBytes;

                // Verifica dimensiunea
                if (imgBytes.Length > 10 * 1024 * 1024)
                {
                    lblMsgAdd.Text = "Fișierul este prea mare (maxim 10 MB).";
                    return;
                }

                // Se apeleaza procedura add_img
                using (OracleCommand cmd = new OracleCommand("add_img", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.BindByName = true;

                    cmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = int.Parse(tbAddId.Text);
                    cmd.Parameters.Add("p_nume_comun", OracleDbType.Varchar2).Value = tbNumeComun.Text;
                    cmd.Parameters.Add("p_specie", OracleDbType.Varchar2).Value = tbSpecie.Text;
                    cmd.Parameters.Add("p_familie", OracleDbType.Varchar2).Value = tbFamilie.Text;
                    cmd.Parameters.Add("p_habitat", OracleDbType.Varchar2).Value = tbHabitat.Text;
                    cmd.Parameters.Add("p_blob", OracleDbType.Blob).Value = imgBytes;
                    cmd.Parameters.Add("p_tip", OracleDbType.Varchar2).Value = fuImagine.PostedFile.ContentType;

                    cmd.CommandTimeout = 30; // max 30 secunde

                    con.Open();
                    cmd.ExecuteNonQuery();

                    // Generarea semnaturii imediat dupa inserare
                    using (OracleCommand genCmd = new OracleCommand("generate_signature_img", con))
                    {
                        genCmd.CommandType = CommandType.StoredProcedure;
                        genCmd.BindByName = true;
                        genCmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = int.Parse(tbAddId.Text);
                        genCmd.CommandTimeout = 60; 
                        genCmd.ExecuteNonQuery();
                    }

                    con.Close();
                }

                lblMsgAdd.Text = "Imagine adăugată și semnătura generată cu succes!";
            }
            catch (Exception ex)
            {
                lblMsgAdd.Text = "Eroare la inserare: " + ex.Message;
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        // METODA COMUNA PENTRU AFISARE IMAGINE
        private void AfiseazaImagine(int id, Image imgControl, Label lbl)
        {
            OracleBlob blob = null;
            try
            {
                // Se apeleaza procedura getcontent_img
                OracleCommand cmd = new OracleCommand("getcontent_img", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = id;

                OracleParameter p_blob = new OracleParameter("p_blob", OracleDbType.Blob);
                p_blob.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p_blob);

                con.Open();
                cmd.ExecuteNonQuery();

              
                blob = (OracleBlob)p_blob.Value;
                byte[] bytes = new byte[blob.Length];
                blob.Read(bytes, 0, (int)blob.Length);

                con.Close();

                // Se converteste in base64 si se afiseaza pe pagina
                string base64String = Convert.ToBase64String(bytes);
                imgControl.ImageUrl = "data:image/jpeg;base64," + base64String;
                lbl.Text = "Imagine afișată.";
            }
            catch (Exception ex)
            {
                lbl.Text = "Eroare afișare: " + ex.Message;
                if (con.State == ConnectionState.Open) con.Close();
            }
            finally
            {
                blob?.Close();
            }
        }

        // AFIȘARE IMAGINE
        protected void btnShow_Click(object sender, EventArgs e)
        {
            AfiseazaImagine(int.Parse(tbShowId.Text), imgPreview, lblMsgShow);
        }

        // REDIMENSIONARE IMAGINE
        protected void btnResize_Click(object sender, EventArgs e)
        {
            try
            {
                OracleCommand cmd = new OracleCommand("resize_img", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = int.Parse(tbResizeId.Text);
                cmd.Parameters.Add("p_w", OracleDbType.Int32).Value = int.Parse(tbW.Text);
                cmd.Parameters.Add("p_h", OracleDbType.Int32).Value = int.Parse(tbH.Text);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                lblMsgResize.Text = "Imagine redimensionată!";
                AfiseazaImagine(int.Parse(tbResizeId.Text), imgResizePreview, lblMsgResize);
            }
            catch (Exception ex)
            {
                lblMsgResize.Text = "Eroare redimensionare: " + ex.Message;
                if (con.State == ConnectionState.Open) con.Close();
            }
        }

        // ROTIRE IMAGINE
        protected void btnRotate_Click(object sender, EventArgs e)
        {
            try
            {
                OracleCommand cmd = new OracleCommand("rotate_img", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = int.Parse(tbRotateId.Text);
                cmd.Parameters.Add("p_angle", OracleDbType.Int32).Value = int.Parse(tbAngle.Text);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                lblMsgRotate.Text = "Imagine rotită!";
                AfiseazaImagine(int.Parse(tbRotateId.Text), imgRotatePreview, lblMsgRotate);
            }
            catch (Exception ex)
            {
                lblMsgRotate.Text = "Eroare rotire: " + ex.Message;
                if (con.State == ConnectionState.Open) con.Close();
            }
        }

        // AJUSTARE LUMINOZITATE
        protected void btnGamma_Click(object sender, EventArgs e)
        {
            try
            {
                // VALIDARE VALOARE GAMMA
                if (!double.TryParse(tbGamma.Text, out double gamma))
                {
                    lblMsgGamma.Text = "Introdu o valoare numerică validă pentru gamma.";
                    return;
                }

                if (gamma < 0.2 || gamma > 5.0)
                {
                    lblMsgGamma.Text = "Valoarea gamma trebuie să fie între 0.2 și 5.0.";
                    return;
                }

                OracleCommand cmd = new OracleCommand("adjust_brightness_img", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = int.Parse(tbGammaId.Text);
                cmd.Parameters.Add("p_gamma", OracleDbType.Double).Value = gamma;

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                lblMsgGamma.Text = $"Luminozitate ajustată (gamma = {gamma}).";
                AfiseazaImagine(int.Parse(tbGammaId.Text), imgGammaPreview, lblMsgGamma);
            }
            catch (Exception ex)
            {
                lblMsgGamma.Text = "Eroare ajustare: " + ex.Message;
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

      

        // EXPORT IMAGINE 
        protected void btnExport_Click(object sender, EventArgs e)
        {
            OracleBlob blob = null;
            try
            {
                int id = int.Parse(tbExportId.Text);

                // Se apeleaza procedura din Oracle
                OracleCommand cmd = new OracleCommand("export_img", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_img_id", OracleDbType.Int32).Value = id;

                OracleParameter p_blob = new OracleParameter("p_blob", OracleDbType.Blob);
                p_blob.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p_blob);

                con.Open();
                cmd.ExecuteNonQuery();

                // Se citeste BLOB-ul rezultat
                blob = (OracleBlob)p_blob.Value;
                byte[] bytes = new byte[blob.Length];
                blob.Read(bytes, 0, (int)blob.Length);
                con.Close();

                // Se creaza un fisier de descarcare
                string fileName = $"pasare_{id}.jpg";
                Response.Clear();
                Response.ContentType = "image/jpeg";
                Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
                Response.BinaryWrite(bytes);
                Response.End();

                lblMsgExport.Text = "Imagine descărcată.";
            }
            catch (ThreadAbortException)
            {
            
            }
            catch (Exception ex)
            {
                lblMsgExport.Text = "Eroare export: " + ex.Message;
                if (con.State == ConnectionState.Open) con.Close();
            }
            finally
            {
                blob?.Close();
            }
        }

        // ADAUGARE FISIER AUDIO
        protected void btnAddAudio_Click(object sender, EventArgs e)
        {
            try
            {
                // Verifica daca s-a selectat un fisier audio
                if (!fuAudio.HasFile)
                {
                    lblMsgAddAudio.Text = " Selectează un fișier audio.";
                    return;
                }

                // Citeste fisierul ca byte[]
                byte[] audioBytes = fuAudio.FileBytes;
                double durataSecunde = 0;

                // Se calculeaza durata audio folosind NAudio
                try
                {
                    using (var ms = new MemoryStream(audioBytes))
                    {
                        using (var reader = new Mp3FileReader(ms))
                        {
                            durataSecunde = reader.TotalTime.TotalSeconds;
                        }
                    }
                }
                catch
                {
                    durataSecunde = 0; 
                }

                // Se apeleaza procedura din Oracle
                using (OracleCommand cmd = new OracleCommand("add_audio", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.BindByName = true;

                    cmd.Parameters.Add("p_aud_id", OracleDbType.Int32).Value = int.Parse(tbAudId.Text);
                    cmd.Parameters.Add("p_titlu", OracleDbType.Varchar2).Value = tbTitluAud.Text;
                    cmd.Parameters.Add("p_nume_comun", OracleDbType.Varchar2).Value = tbNumeComAud.Text;
                    cmd.Parameters.Add("p_specie", OracleDbType.Varchar2).Value = tbSpecieAud.Text;
                    cmd.Parameters.Add("p_blob", OracleDbType.Blob).Value = audioBytes;
                    cmd.Parameters.Add("p_tip", OracleDbType.Varchar2).Value = fuAudio.PostedFile.ContentType;
                    cmd.Parameters.Add("p_durata", OracleDbType.Decimal).Value = durataSecunde;

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    lblMsgAddAudio.Text = $"Fișierul audio a fost adăugat cu succes!<br />🎵 Durată: {durataSecunde:F2} secunde.";
                }

                if (FindControl("lblDurataAudio") is Label lblDurata)
                {
                    lblDurata.Text = $"Durată: {durataSecunde:F2} secunde";
                }
            }
            catch (Exception ex)
            {
                lblMsgAddAudio.Text = "Eroare la inserare: " + ex.Message;
                if (con.State == ConnectionState.Open) con.Close();
            }
        }
        
        // REDARE FISIER AUDIO
        protected void btnPlayAudio_Click(object sender, EventArgs e)
        {
            OracleBlob blob = null;

            try
            {
                int id = int.Parse(tbPlayId.Text);

                // Se apeleaza procedura
                OracleCommand cmd = new OracleCommand("get_audio", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;

                cmd.Parameters.Add("p_aud_id", OracleDbType.Int32).Value = id;

                OracleParameter p_blob = new OracleParameter("p_blob", OracleDbType.Blob);
                p_blob.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p_blob);

                con.Open();
                cmd.ExecuteNonQuery();
                blob = (OracleBlob)p_blob.Value;

                // Se citeste fisierul audio (BLOB)
                byte[] bytes = new byte[blob.Length];
                blob.Read(bytes, 0, (int)blob.Length);
                con.Close();

                // Se salveaza temporar pe server
                string fileName = $"audio_{id}.mp3";
                string folder = Server.MapPath("~/temp/");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string filePath = Path.Combine(folder, fileName);
                File.WriteAllBytes(filePath, bytes);

                string virtualPath = "~/temp/" + fileName;

                // Se genereaza HTML <audio controls> pt redare si se incarca playerul in pagina web
                litAudioPlayer.Text = $"<audio controls style='width:100%;'><source src='{ResolveUrl(virtualPath)}' type='audio/mpeg'>Browserul tău nu suportă redarea audio.</audio>";

                lblMsgPlayAudio.Text = "🎵 Fișierul audio poate fi redat mai jos.";
            }
            catch (Exception ex)
            {
                lblMsgPlayAudio.Text = "Eroare redare: " + ex.Message;
                litAudioPlayer.Text = string.Empty;
                if (con.State == ConnectionState.Open) con.Close();
            }
            finally
            {
                blob?.Dispose();
            }
        }

        // DESCARCARE FISIER AUDIO
        protected void btnDownloadAudio_Click(object sender, EventArgs e)
        {
            try
            {
                // Se verifica daca a fost introdus id ul
                if (string.IsNullOrWhiteSpace(tbDownloadAudId.Text))
                {
                    lblMsgDownloadAudio.Text = "Introdu ID-ul fișierului audio.";
                    return;
                }

                int id = int.Parse(tbDownloadAudId.Text);

                // Se apeleaza procedura
                using (OracleCommand cmd = new OracleCommand("get_audio", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.BindByName = true;

                    cmd.Parameters.Add("p_aud_id", OracleDbType.Int32).Value = id;

                    OracleParameter p_blob = new OracleParameter("p_blob", OracleDbType.Blob);
                    p_blob.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p_blob);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    OracleBlob blob = (OracleBlob)p_blob.Value;
                    if (blob == null || blob.Length == 0)
                    {
                        con.Close();
                        lblMsgDownloadAudio.Text = "Fișierul audio nu a fost găsit.";
                        return;
                    }

                    // Se citeste fisierul BLOB
                    byte[] audioBytes = new byte[blob.Length];
                    blob.Read(audioBytes, 0, (int)blob.Length);
                    con.Close();

                    // Se detecteaza tipul fisierului pt extensie
                    string tipFisier;
                    string extensie;
                    using (var cmd2 = new OracleCommand("SELECT tip_fisier FROM audio_pasari WHERE aud_id = :id", con))
                    {
                        cmd2.Parameters.Add(":id", id);
                        con.Open();
                        tipFisier = cmd2.ExecuteScalar()?.ToString() ?? "audio/mpeg";
                        con.Close();
                    }

                    if (tipFisier.Contains("wav")) extensie = ".wav";
                    else if (tipFisier.Contains("ogg")) extensie = ".ogg";
                    else extensie = ".mp3";

                    string fileName = $"audio_{id}{extensie}";

                    // Se trimite fisierul catre browser
                    Response.Clear();
                    Response.ContentType = tipFisier;
                    Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
                    Response.BinaryWrite(audioBytes);
                    Response.End();
                }

                lblMsgDownloadAudio.Text = "Descărcare începută.";
            }
            catch (ThreadAbortException)
            {
              
            }
            catch (Exception ex)
            {
                lblMsgDownloadAudio.Text = "Eroare descărcare: " + ex.Message;
                if (con.State == ConnectionState.Open) con.Close();
            }
        }

        // RECUNOASTEREA SEMANTICA A IMAGINILOR
        protected void btnFindSimilar_Click(object sender, EventArgs e)
        {
            try
            {
                // Verifica daca s-a incarcat o imagine
                if (!fuImagineSem.HasFile)
                {
                    lblMsgSem.Text = "Selectează o imagine pentru comparare.";
                    return;
                }

                // Preia imaginea incarcata
                byte[] imgBytes = fuImagineSem.FileBytes;

                // Afisarea imaginii introduse
                string base64Input = Convert.ToBase64String(imgBytes);
                imgInitiala.ImageUrl = "data:image/jpeg;base64," + base64Input;

                // Citirea ponderilor introduse de utilizator
                decimal cCuloare = decimal.Parse(tbCuloare.Text);
                decimal cTextura = decimal.Parse(tbTextura.Text);
                decimal cForma = decimal.Parse(tbForma.Text);
                decimal cLocatie = decimal.Parse(tbLocatie.Text);

                int idRezultat = 0;
                double scorFinal = 0;

                // Apel procedura Oracle + scorul de similaritate
                using (OracleCommand cmd = new OracleCommand("psregasire_pasari", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.BindByName = true;

                    cmd.Parameters.Add("fis", OracleDbType.Blob).Value = imgBytes;
                    cmd.Parameters.Add("cculoare", OracleDbType.Decimal).Value = cCuloare;
                    cmd.Parameters.Add("ctextura", OracleDbType.Decimal).Value = cTextura;
                    cmd.Parameters.Add("cforma", OracleDbType.Decimal).Value = cForma;
                    cmd.Parameters.Add("clocatie", OracleDbType.Decimal).Value = cLocatie;

                    OracleParameter p_idrez = new OracleParameter("idrez", OracleDbType.Int32);
                    p_idrez.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p_idrez);

                    OracleParameter p_scor = new OracleParameter("p_scor", OracleDbType.Double);
                    p_scor.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p_scor);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    idRezultat = Convert.ToInt32(p_idrez.Value.ToString());
                    scorFinal = Convert.ToDouble(p_scor.Value.ToString());
                }

                if (idRezultat == 0)
                {
                    lblMsgSem.Text = "Nu a fost găsită o imagine similară.";
                    return;
                }

                // Imaginea rezultata
                OracleCommand cmdGet = new OracleCommand("getcontent_img", con);
                cmdGet.CommandType = CommandType.StoredProcedure;
                cmdGet.Parameters.Add("p_img_id", OracleDbType.Int32).Value = idRezultat;

                OracleParameter p_blob = new OracleParameter("p_blob", OracleDbType.Blob);
                p_blob.Direction = ParameterDirection.Output;
                cmdGet.Parameters.Add(p_blob);

                con.Open();
                cmdGet.ExecuteNonQuery();

                OracleBlob blob = (OracleBlob)p_blob.Value;
                byte[] bytes = new byte[blob.Length];
                blob.Read(bytes, 0, (int)blob.Length);
                con.Close();

                string base64Result = Convert.ToBase64String(bytes);
                imgRezultatSem.ImageUrl = "data:image/jpeg;base64," + base64Result;

                // Detalii despre imagine
                string numeInit = "", specieInit = "", familieInit = "", habitatInit = "";

                using (OracleCommand cmdDet = new OracleCommand(
                    "SELECT nume_comun, specie, familie, habitat " +
                    "FROM img_pasari WHERE img_id = :id", con))
                {
                    cmdDet.Parameters.Add(":id", OracleDbType.Int32).Value = idRezultat;

                    con.Open();
                    using (OracleDataReader r = cmdDet.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            numeInit = r.GetString(0);
                            specieInit = r.GetString(1);
                            familieInit = r.GetString(2);
                            habitatInit = r.GetString(3);
                        }
                    }
                    con.Close();
                }

                // Afisarea unui mesaj si a detaliilor despre pasarea identificata
                lblDetaliiInitiala.Text =
                    $"<b style='color:green;'>A fost identificată pasărea din imagine!</b><br/><br/>" +
                    $"<b>{numeInit}</b><br/>Specie: {specieInit}<br/>Familie: {familieInit}<br/>Habitat: {habitatInit}";

                // Afisarea scorului de similaritate
                lblScor.Text = $"Scor de similaritate (Oracle): {Math.Round(scorFinal, 2)}";
                lblMsgSem.Text = $"Imaginea cea mai asemănătoare are ID = {idRezultat}.";
            }
            catch (Exception ex)
            {
                lblMsgSem.Text = "Eroare: " + ex.Message;
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        // CAUTARE AUDIO DUPA NUMELE PASARII
        protected void btnCautaAudio_Click(object sender, EventArgs e)
        {
            try
            {
                string textCautat = tbCautaAudio.Text.Trim();
                if (string.IsNullOrEmpty(textCautat))
                {
                    lblMsgSearchAudio.Text = "Introdu un nume de pasăre pentru căutare.";
                    return;
                }

                // Se construieste o interogare SQL
                string query = @"
            SELECT aud_id, titlu, nume_comun, specie, blob_data, tip_fisier
            FROM audio_pasari
            WHERE LOWER(nume_comun) LIKE LOWER(:nume)";

                var da = new OracleDataAdapter(query, con);
                da.SelectCommand.Parameters.Add("nume", $"%{textCautat}%");

                // Se completeaza un data table cu rezultatele
                var dt = new DataTable();
                da.Fill(dt);

                dt.Columns.Add("AUDIO_PLAYER", typeof(string));

                if (dt.Rows.Count == 0)
                {
                    gvAudio.DataSource = null;
                    gvAudio.DataBind();
                    lblMsgSearchAudio.Text = "Nicio înregistrare audio găsită.";
                    return;
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (row["BLOB_DATA"] == DBNull.Value) continue;

                    // Se converteste fiecare BLOB in base64
                    byte[] audioBytes = (byte[])row["BLOB_DATA"];
                    string tip = row["TIP_FISIER"] == DBNull.Value ? "audio/mpeg" : row["TIP_FISIER"].ToString();
                    string base64 = Convert.ToBase64String(audioBytes);

                    // Se creaza un player pt fiecare rand
                    string playerHtml =
                        $"<audio controls style='width:240px'>" +
                        $"<source src='data:{tip};base64,{base64}' type='{tip}'>" +
                        "Browserul tău nu suportă redarea audio." +
                        "</audio>";

                    row["AUDIO_PLAYER"] = playerHtml;

                }

                // Se afiseaza totul intr-un grid view
                gvAudio.DataSource = dt;
                gvAudio.DataBind();

                lblMsgSearchAudio.Text = $"Au fost găsite {dt.Rows.Count} fișiere audio.";

            }
            catch (Exception ex)
            {
                lblMsgSearchAudio.Text = "Eroare la căutare: " + ex.Message;
            }
        }
    }
}

