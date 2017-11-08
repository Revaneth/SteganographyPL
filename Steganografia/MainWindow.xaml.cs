using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace Steganografia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int startByte;
        private BitArray header;
        private long maxChars;

        private string fileName = "";

        public MainWindow()
        {
            startByte = 200;
            header = new BitArray(new int[] { Convert.ToInt32("11010100010001001100101000100100", 2) });

            InitializeComponent();
        }

        private void selectFileButton_Click(object sender, RoutedEventArgs e)
        {
            fileName = "";
            selectedFileLabel.Content = "";
            maxCharsLabel.Content = "";
            readDataButton.IsEnabled = saveDataButton.IsEnabled = false;


            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "mapa bitowa (*.bmp)|*.bmp";
            dialog.Title = "Wybierz plik";
            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {

                FileInfo f = new FileInfo(dialog.FileName);
                maxChars = (f.Length - startByte - header.Length - 32) / 8;

                if (maxChars <= 0)
                {
                    MessageBox.Show("Obrazek jest za mały by cokolwiek w nim zapisać!");
                    return;
                }

                fileName = dialog.FileName;
                selectedFileLabel.Content = "Obraz: " + dialog.FileName;
                maxCharsLabel.Content = textBlock.Text.Length.ToString() + "/" + maxChars.ToString() + " znaków";
                readDataButton.IsEnabled = saveDataButton.IsEnabled = true;
            }
        }

        private void textBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (fileName != "")
                maxCharsLabel.Content = textBlock.Text.Length.ToString() + "/" + maxChars.ToString() + " znaków";

            maxCharsLabel.Foreground = textBlock.Text.Length > maxChars ? Brushes.Red : Brushes.Black;
        }

        private void readDataButton_Click(object sender, RoutedEventArgs e)
        {
            FileStream fs;
            byte[] fileBytes;

            fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            fileBytes = br.ReadBytes((int)fs.Length);
            br.Close();
            fs.Close();


            int i = startByte;


            //Odczytanie nagłówka
            foreach (bool bit in header)
            {
                if ((bit ? 1 : 0) != (fileBytes[i] & (byte)1))
                {
                    MessageBox.Show("Brak danych zapisanych w obrazie");
                    return;
                }

                i++;
            }


            //Odczytanie długości danych (32 bity)
            BitArray dataLengthBitArray = new BitArray(32);

            for (int j = 0; j < dataLengthBitArray.Length; j++, i++)
            {
                dataLengthBitArray[j] = (fileBytes[i] & (byte)1) == 1;
            }

            int[] dataLengthIntArray = new int[1];
            dataLengthBitArray.CopyTo(dataLengthIntArray, 0);
            int dataLength = dataLengthIntArray[0];


            //Odczyt danych
            BitArray dataBits = new BitArray(dataLength);

            for (int j = 0; j < dataBits.Length; j++, i++)
            {
                //Przerywamy odczyt jak kończy nam się tablica z danymi
                if (i >= fileBytes.Length)
                    break;

                dataBits[j] = (fileBytes[i] & (byte)1) == 1;
            }

            byte[] dataBytes = new byte[dataBits.Length / 8];
            dataBits.CopyTo(dataBytes, 0);


            textBlock.Text = System.Text.Encoding.ASCII.GetString(dataBytes);
            MessageBox.Show("Odczytano dane z obrazu!");
        }

        private void saveDataButton_Click(object sender, RoutedEventArgs e)
        {
            FileStream fs;
            byte[] fileBytes;

            fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            fileBytes = br.ReadBytes((int)fs.Length);
            br.Close();
            fs.Close();


            int i = startByte;


            //Zapisanie nagłówka
            foreach (bool bit in header)
            {
                if (bit)
                {
                    fileBytes[i] = (byte)(fileBytes[i] | (byte)1);
                }
                else
                {
                    fileBytes[i] = (byte)(fileBytes[i] & (byte)254);
                }

                i++;
            }


            //Zapisanie długości danych (32 bity)
            BitArray dataLength = new BitArray(new int[] { textBlock.Text.Length * 8 });

            foreach (bool bit in dataLength)
            {
                if (bit)
                {
                    fileBytes[i] = (byte)(fileBytes[i] | (byte)1);
                }
                else
                {
                    fileBytes[i] = (byte)(fileBytes[i] & (byte)254);
                }

                i++;
            }

            //Zapisanie danych (długość nie znana do końca :) )
            BitArray dataBits = new BitArray(UTF8Encoding.Default.GetBytes(textBlock.Text));

            foreach (bool bit in dataBits)
            {
                //Przerywamy zapis jak kończy nam się miejsce
                if (i >= fileBytes.Length)
                    break;

                if (bit)
                {
                    fileBytes[i] = (byte)(fileBytes[i] | (byte)1);
                }
                else
                {
                    fileBytes[i] = (byte)(fileBytes[i] & (byte)254);
                }

                i++;
            }


            //Zapis do pliku
            fs = new FileStream(fileName.Substring(0, fileName .Length - 4) + "_2.bmp", FileMode.OpenOrCreate, FileAccess.Write);
            fs.SetLength(0);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(fileBytes);
            bw.Close();
            fs.Close();

            textBlock.Text = "";
            MessageBox.Show("Zapisano dane w obrazie!");
        }
    }
}
