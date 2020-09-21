using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Odbc;

namespace Proyecto_Transaction_Log
{
    public partial class Form1 : Form
    {
        OdbcConnection connection;
        OdbcCommand command;
        OdbcDataAdapter dataAdapter;
        DataTable dataResult;
        DataRow row;
        int posFixedLength = 0;
        int countList;
        Int16 posCountFieldVariable;
        List<Int16> posEveryVariableField;
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {


        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }


        private string toString(byte[] data)
        {
            string aux = Encoding.UTF8.GetString(data);
;           string result = String.Empty;
            int auxInt;
            for (int i = 0; i < aux.Length; i++)
            {
                auxInt = aux[i];
                if (auxInt > 31 && auxInt < 127)
                {
                    result += aux[i];
                }
            }
            return result;
        }

        private int toInt(byte[] data) {
            return BitConverter.ToInt32(data, 0);
        }
   

        private long toBigInt(byte[] data)
        {
            return BitConverter.ToInt64(data, 0);
        }

        private Int16 toTinyInt(byte[] data)
        {
            return BitConverter.ToInt16(data, 0);
        }

        private decimal toDecimal(byte[] data, Int16 point)
        {
            string aux;
            byte[] auxFixed;
            int pos;
            if (data.Length < 16)
            {
                auxFixed = new byte[16];
                for (int i = 0; i < 16; i++)
                {
                    if (i >= data.Length)
                    {
                        auxFixed[i] = 0x00;
                    }
                    else
                    {
                        auxFixed[i] = data[i];
                    }
                }
                aux = BitConverter.ToUInt64(auxFixed,0).ToString();
            }
            else {
                aux = BitConverter.ToUInt64(data, 0).ToString();
            }
            point += point;
            pos = aux.Length;
            pos -= point;          
            return Convert.ToDecimal(aux.Insert(pos, "."));
        }

        private decimal toMoney(byte[] data)
        {
            string aux = BitConverter.ToUInt64(data, 0).ToString();
            int pos = aux.Length - 4;
            return Convert.ToDecimal(aux.Insert(pos, "."));
        }


        private Int64 toFloat(byte[] data)
        {
            return Convert.ToInt64(data);
        }


        private char toChar(byte data)
        {
            return Convert.ToChar(data);
        }
        
        private float toReal(byte[] data)
        {
            double auxDouble=0;     
                byte[] auxFixed;
                if (data.Length < 8)
                {
                    auxFixed = new byte[8];
                    for (int i = 0; i < 8; i++)
                    {
                        if (i >= data.Length)
                        {
                            auxFixed[i] = 0x00;
                        }
                        else
                        {
                            auxFixed[i] = data[i];
                        }
                    }
                    auxDouble=BitConverter.ToDouble(auxFixed, 0);
                }
                else
                {
                    auxDouble = BitConverter.ToDouble(data, 0);
                }
            return Convert.ToSingle(auxDouble);
        }

        private Int16 toBit(byte data)
        {
            return Convert.ToInt16(data);

        }


        private string toBinary(byte[] data)
        {
            return  "0x" + BitConverter.ToString(data).Replace("-","");
        }

        private DateTime toDateTime(byte[] data)
        {
            return new DateTime(1960, 01, 01, 01, 01, 01, DateTimeKind.Local).AddMilliseconds(BitConverter.ToInt64(data, 0)/100);
        }


        private DateTime toSmallDateTime(byte[] data)
        {
            return new DateTime(1970, 01, 01).AddSeconds(BitConverter.ToInt32(data, 0));
        }


        private void btnEjecutar_Click(object sender, EventArgs e)
        {
            string data = this.txtQuery.Text;
            string[] param;
            if (data.Length == 0)
            {
                MessageBox.Show("El campo esta vacio");
                return;
            }
            param = data.Split(' ');
            if (param[0].Equals("show"))
                ExecuteAnalyzer(e, data);
            else if (param[0].ToLower().Equals("lstinsert"))
            {
                SendQueryLst(e, "LOP_INSERT_ROWS");
            }
            else if (param[0].ToLower().Equals("lstupdate"))
            {
                SendQueryLst(e, "LOP_MODIFY_ROW");
            }
            else if (param[0].ToLower().Equals("lstdelete"))
            {
                SendQueryLst(e, "LOP_DELETE_ROWS");
            }
            else
            {
                SendQueryCRUD(e, data);
            }
        }

        private void ExecuteAnalyzer(EventArgs e, string param)
        {
            string[] words = param.Split(' ');
            string transactionType = String.Empty;
            if (words.Length == 1)
            {
                MessageBox.Show("Id de transaccion vacio");
                return;
            }
            connection = new OdbcConnection();
            string cs = "DRIVER={ODBC Driver 17 for SQL Server};" +
                "SERVER=ENRIQUECS\\SQLEXPRESS;DATABASE=proyecto2;" +
                "Trusted_Connection=Yes";
            string query = "SELECT [Current LSN]," +
                            "Operation," +
                            "[Transaction ID]," +
                            "[AllocUnitName]," +
                            "[RowLog Contents 0]," +
                            "[RowLog Contents 1]," +
                            "[RowLog Contents 2]," +
                            "[RowLog Contents 3]," +
                            "[RowLog Contents 4]," +
                            "[RowLog Contents 5] " +
                            "FROM sys.fn_dblog" +
                            "(NULL, NULL) " +
                            "WHERE [Current LSN] in ('" +
                            words[1] + "');";
            connection.ConnectionString = cs;
            try
            {
                connection.Open();
                dataAdapter = new OdbcDataAdapter(query, connection);
                DataTable table = new DataTable();
                dataAdapter.Fill(table);
                transactionType = table.Rows[0]["Operation"].ToString();
                if (transactionType.Equals("LOP_INSERT_ROWS"))
                {
                    TransactInsert(table);
                }
                else if (transactionType.Equals("LOP_MODIFY_ROW"))
                {
                    TransactUpdate(table);
                }
                else if (transactionType.Equals("LOP_DELETE_ROWS"))
                {
                    MessageBox.Show("Los siguientes datos se eliminaron:");
                    TransactInsert(table);
                }
                else
                {
                    MessageBox.Show("Transaccion fuera del rango");
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error en Query" + "\nDetalle: " + error.ToString());
            }
        }

        private void SendQueryCRUD(EventArgs e, string query)
        {
            connection = new OdbcConnection();
            string cs = "DRIVER={ODBC Driver 17 for SQL Server};" +
                "SERVER=ENRIQUECS\\SQLEXPRESS;DATABASE=proyecto2;" +
                "Trusted_Connection=Yes";
            connection.ConnectionString = cs;
            try
            {
                connection.Open();
                command = new OdbcCommand(query, connection);
                command.ExecuteNonQuery();
                MessageBox.Show("Query realizado");
            }
            catch (Exception error)
            {
                MessageBox.Show("Error en Query" + "\nDetalle: " + error.ToString());
            }
        }

        private void SendQueryLst(EventArgs e, string transactType)
        {
            connection = new OdbcConnection();
            string cs = "DRIVER={ODBC Driver 17 for SQL Server};" +
                "SERVER=ENRIQUECS\\SQLEXPRESS;DATABASE=proyecto2;" +
                "Trusted_Connection=Yes";
            connection.ConnectionString = cs;
            string query= " SELECT [Current LSN],"+
                           "Operation, "+
                           "[Transaction ID],"+ 
                            "[AllocUnitName],"+
	                        "[RowLog Contents 0],"+
	                        "[RowLog Contents 1],"+
	                        "[RowLog Contents 2],"+
 	                        "[RowLog Contents 3],"+
 	                        "[RowLog Contents 4],"+
 	                        "[RowLog Contents 5] "+
                            "FROM sys.fn_dblog"+
                            "(NULL, NULL) "+
                            "WHERE operation IN"+
                            "('"+ transactType+"')";
            List<string> content0= new List<string>();
            List<string> content1= new List<string>();
            List<string> content2= new List<string>();
            List<string> content3= new List<string>();
            List<string> content4= new List<string>();
            List<string> content5= new List<string>();
            try
            {
                connection.Open();
                dataAdapter = new OdbcDataAdapter(query, connection);
                DataTable table = new DataTable();
                dataAdapter.Fill(table);
                DataColumn column = new DataColumn();
                for (int i = 0; i <= table.Rows.Count-1; i++)
                {
                    for (int j = 0; j <= table.Columns.Count-1; j++)
                    {
                        if (table.Columns[j].ColumnName.Equals("RowLog Contents 0")) {                                                    
                            content0.Add("0x"+ BitConverter.ToString((byte[])table.Rows[i][j]).Replace("-", ""));
                        }
                        if (table.Columns[j].ColumnName.Equals("RowLog Contents 1"))
                        {
                            content1.Add("0x" + BitConverter.ToString((byte[])table.Rows[i][j]).Replace("-", ""));
                        }
                        if (table.Columns[j].ColumnName.Equals("RowLog Contents 2"))
                        {
                            content2.Add("0x" + BitConverter.ToString((byte[])table.Rows[i][j]).Replace("-", ""));
                        }
                        if (table.Columns[j].ColumnName.Equals("RowLog Contents 3"))
                        {
                            content3.Add("0x" + BitConverter.ToString((byte[])table.Rows[i][j]).Replace("-", ""));
                        }
                        if (table.Columns[j].ColumnName.Equals("RowLog Contents 4"))
                        {
                            content4.Add("0x" + BitConverter.ToString((byte[])table.Rows[i][j]).Replace("-", ""));
                        }
                        if (table.Columns[j].ColumnName.Equals("RowLog Contents 5"))
                        {
                            content5.Add("0x" + BitConverter.ToString((byte[])table.Rows[i][j]).Replace("-", ""));
                        }
                    }
                }

                for (int i = table.Columns.Count - 1; i >= 0; i--)
                {
                    string name = table.Columns[i].ColumnName.ToString();
                    if (table.Columns[i].ColumnName.Equals("RowLog Contents 0") ||
                        table.Columns[i].ColumnName.Equals("RowLog Contents 1") ||
                        table.Columns[i].ColumnName.Equals("RowLog Contents 2") ||
                        table.Columns[i].ColumnName.Equals("RowLog Contents 3") ||
                        table.Columns[i].ColumnName.Equals("RowLog Contents 4") ||
                        table.Columns[i].ColumnName.Equals("RowLog Contents 5"))
                    {
                        table.Columns.Remove(name);
                    }
                }
                table.Columns.Add("RowLog Contents 0", typeof(string));
                table.Columns.Add("RowLog Contents 1", typeof(string));
                table.Columns.Add("RowLog Contents 2", typeof(string));
                table.Columns.Add("RowLog Contents 3", typeof(string));
                table.Columns.Add("RowLog Contents 4", typeof(string));
                table.Columns.Add("RowLog Contents 5", typeof(string));                
                for (int i = 0; i <= table.Rows.Count - 1; i++)
                {
                    table.Rows[i]["RowLog Contents 0"] = content0[i];
                    table.Rows[i]["RowLog Contents 1"] = content1[i];
                    table.Rows[i]["RowLog Contents 2"] = content2[i];
                    table.Rows[i]["RowLog Contents 3"] = content3[i];
                    table.Rows[i]["RowLog Contents 4"] = content4[i];
                    table.Rows[i]["RowLog Contents 5"] = content5[i];
                }
                
                this.lstData.DataSource = table;
                this.lstData.AutoResizeColumns();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error en Query" + "\nDetalle: " + error.ToString());
            }
        }


        private void TransactInsert(DataTable data)
        {
            byte[] dataByte = (byte[])data.Rows[0]["RowLog Contents 0"];
            string tableName = getTableName(data.Rows[0]["AllocUnitName"].ToString());
            DataTable dataFields = getFields(tableName);
            byte[] auxColumnsVariables = new byte[2];
            int fixedLength = 0;
            posCountFieldVariable = 3;
            posFixedLength = 4;
            Int16 countFieldVariable = 0;
            byte[] lengthVariableField=new byte[2];
            string valueType;
            posEveryVariableField = new List<Int16>();
            countList = 0;
            dataResult = new DataTable();
            row = dataResult.NewRow();
            string columnName = String.Empty;
            auxColumnsVariables[0] = dataByte[2];
            auxColumnsVariables[1] = dataByte[3];
            if (dataByte[0].CompareTo(0x30) == 0) {
                posCountFieldVariable += toTinyInt(auxColumnsVariables);
                Array.Copy(dataByte, posCountFieldVariable, lengthVariableField, 0, 2);
                countFieldVariable = toTinyInt(lengthVariableField);
                for (int i = 0; i < countFieldVariable; i++)
                {
                    posCountFieldVariable += 2;
                    Array.Copy(dataByte, posCountFieldVariable, lengthVariableField, 0, 2);
                    posEveryVariableField.Add(toTinyInt(lengthVariableField));
                }
                posCountFieldVariable += 2;
            }
            if (dataFields.Rows.Count > 0)
            {
                for (int i = 0; i < dataFields.Rows.Count; i++)
                {
                    valueType = dataFields.Rows[i][1].ToString();
                    columnName = dataFields.Rows[i][0].ToString();
                    fixedLength = Convert.ToInt32(dataFields.Rows[i][2]);
                    FieldType(valueType, columnName, dataByte, fixedLength);
                }
                dataResult.Rows.Add(row);
                this.lstData.DataSource = dataResult;
                this.lstData.AutoResizeColumns();
            }

        }


        private void FieldType(string valueType, string columnName, byte[]dataByte, int fixedLength) {
            byte[] aux;
            if (valueType.Equals("char"))//esto va a ir aparte
            {
                byte dataChar = dataByte[posFixedLength];
                char result = toChar(dataChar);
                posFixedLength++;
                dataResult.Columns.Add(columnName, typeof(char));
                row[columnName] = result;
            }
            else if (valueType.Contains("varchar"))
            {
                int sizeVariableField = posEveryVariableField[countList] - posCountFieldVariable;
                aux = new byte[sizeVariableField];
                Array.Copy(dataByte, posCountFieldVariable, aux, 0, sizeVariableField);
                string result = toString(aux);
                posCountFieldVariable = posEveryVariableField[countList];
                countList++;
                dataResult.Columns.Add(columnName, typeof(string));
                row[columnName] = result;
            }
            else if (valueType.Equals("datetime"))//ya esta
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                DateTime result = toDateTime(aux);
                dataResult.Columns.Add(columnName, typeof(string));
                row[columnName] = result.ToString();
            }
            else if (valueType.Equals("smalldatetime"))
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                DateTime result = toSmallDateTime(aux);
                dataResult.Columns.Add(columnName, typeof(string));
                row[columnName] = result.ToString();
            }
            else if (valueType.Equals("int"))//ya esta
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                int result = toInt(aux);
                dataResult.Columns.Add(columnName, typeof(int));
                row[columnName] = result;
            }
            else if (valueType.Equals("bigint"))//ya esta
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                long result = toBigInt(aux);
                dataResult.Columns.Add(columnName, typeof(long));
                row[columnName] = result;
            }
            else if (valueType.Equals("tinyint"))//ya esta
            {
                byte a = dataByte[posFixedLength];
                posFixedLength++;
                Int16 result = toBit(a);
                dataResult.Columns.Add(columnName, typeof(Int16));
                row[columnName] = result;
            }
            else if (valueType.Equals("decimal"))//ya esta
            {
                fixedLength--;
                aux = new byte[fixedLength];
                fixedLength--;
                Int16 a = Convert.ToInt16(dataByte[posFixedLength] + 0x00);
                posFixedLength++;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                decimal result = toDecimal(aux, a);
                dataResult.Columns.Add(columnName, typeof(decimal));
                row[columnName] = result;
            }
            else if (valueType.Equals("money"))//ya esta
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                decimal result = toMoney(aux);
                dataResult.Columns.Add(columnName, typeof(decimal));
                row[columnName] = result;
            }
            else if (valueType.Equals("float"))//no funciona
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                Int64 result = toFloat(aux);
                dataResult.Columns.Add(columnName, typeof(Int64));
                row[columnName] = result;
            }
            else if (valueType.Equals("real"))//no funciona
            {
                aux = new byte[fixedLength];
                fixedLength--;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                float result = toReal(aux);
                dataResult.Columns.Add(columnName, typeof(float));
                row[columnName] = result;
            }
            else if (valueType.Equals("numeric"))//ya esta
            {
                fixedLength--;
                aux = new byte[fixedLength];
                fixedLength--;
                Int16 a = Convert.ToInt16(dataByte[posFixedLength]);
                posFixedLength++;
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                fixedLength++;
                posFixedLength += fixedLength;
                decimal result = toDecimal(aux, a);
                dataResult.Columns.Add(columnName, typeof(decimal));
                row[columnName] = result;
            }
            else if (valueType.Equals("bit"))//ya esta
            {
                byte a = dataByte[posFixedLength];
                posFixedLength++;
                Int16 result = toBit(a);
                if (result != 0)
                {
                    result = 1;
                }
                dataResult.Columns.Add(columnName, typeof(byte));
                row[columnName] = result;
            }
            else if (valueType.Equals("binary"))//ya esta
            {
                aux = new byte[fixedLength];
                Array.Copy(dataByte, posFixedLength, aux, 0, fixedLength);
                posFixedLength += fixedLength;
                string result = toBinary(aux);
                dataResult.Columns.Add(columnName, typeof(string));
                row[columnName] = result;
            }

        }
        
        private void TransactUpdate(DataTable data){
            string[] dataQuery = this.txtQuery.Text.Split(' ');
            if (dataQuery.Length == 2) {
                MessageBox.Show("Falta un parametro");
                return;
            }
            byte[] dataLast = (byte[])data.Rows[0]["RowLog Contents 0"];
            byte[] dataNew = (byte[])data.Rows[0]["RowLog Contents 1"];
            string tableName = getTableName(data.Rows[0]["AllocUnitName"].ToString());
            string valueType = String.Empty;
            int range =0;
            DataTable dataFields = getFields(tableName);
            dataResult = new DataTable();
            row = dataResult.NewRow();
            string namefield1 = "Dato anterior";
            string namefield2 = "Dato nuevo";
            for (int i = 0; i <= dataFields.Rows.Count - 1; i++)
            {   
                    if (dataFields.Rows[i][0].ToString().Equals(dataQuery[2]))
                    {
                        valueType = Convert.ToString(dataFields.Rows[i][0+1]);
                        range = Convert.ToInt32(dataFields.Rows[i][0 + 2]);
                        break;
                    }
            }
            if (valueType.Equals("varchar")){
                dataResult.Columns.Add(namefield1, typeof(string));
                row[namefield1] = toString(dataLast);
                dataResult.Columns.Add(namefield2, typeof(string));
                row[namefield2] = toString(dataNew);
            } else {
                if (dataLast.Length < range)
                {
                    posFixedLength = 0;
                    FieldType(valueType, namefield1, fillZeros(dataLast, range), range);
                }
                else {
                    FieldType(valueType, namefield1, dataLast, range);
                }
                if (dataNew.Length < range)
                {
                    posFixedLength = 0;
                    FieldType(valueType, namefield2, fillZeros(dataNew, range), range);
                }
                else
                {
                    FieldType(valueType, namefield2, dataNew, range);
                }
            }
            dataResult.Rows.Add(row);
            this.lstData.DataSource = dataResult;
            this.lstData.AutoResizeColumns();
        }

        private byte[] fillZeros(byte[] data, int range) {
            byte[] auxFixed = new byte[range];
            for (int i = 0; i < range - 1; i++)
            {
                if (i >= data.Length)
                {
                    auxFixed[i] = 0x00;
                }
                else
                {
                    auxFixed[i] = data[i];
                }
            }
            return auxFixed;
        }

        private string getTableName(string name)
        {
            string[] data;
            for (int i = 0; i < name.Length - 1; i++)
            {
                if (name[i] == 46)
                {
                    data = name.Split('.');
                    return data[1];
                }
            }
            return null;
        }

        private DataTable getFields(string tableName)
        {
            connection = new OdbcConnection();
            string cs = "DRIVER={ODBC Driver 17 for SQL Server};" +
                "SERVER=ENRIQUECS\\SQLEXPRESS;DATABASE=proyecto2;" +
                "Trusted_Connection=Yes";
            string query = "SELECT syscolumns.name AS column_name," +
                           "systypes.name AS datatype, syscolumns.LENGTH " +
                           "AS LENGTH FROM sysobjects INNER JOIN " +
                           "syscolumns ON sysobjects.id = syscolumns.id INNER JOIN " +
                           "systypes ON syscolumns.xtype = systypes.xtype " +
                           "WHERE(systypes.name != 'sysname') and(sysobjects.name =" +
                           "'" + tableName + "') "+
                           "order by syscolumns.colorder asc";
            connection.ConnectionString = cs;
            try
            {
                connection.Open();
                dataAdapter = new OdbcDataAdapter(query, connection);
                DataTable table = new DataTable();
                dataAdapter.Fill(table);
                //this.lstData.DataSource = table;
                return table;
            }
            catch (Exception error)
            {
                MessageBox.Show("Error en Query" + "\nDetalle: " + error.ToString());
            }
            return null;
        }
    }
}


/*
            string hex = "AC31011BB6C2";
            if (hex.Length % 2 != 0)
                throw new Exception("La cadena no puede ser impar");

            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                result[i] = byte.Parse(hex.Substring(2 * i, 2), System.Globalization.NumberStyles.HexNumber);
            }
            string result2 =result.ToString();
            MessageBox.Show(result2);
            

string hexDate = "AC31011BB6C2";
long secondsAfterEpoch = Int64.Parse(hexDate, System.Globalization.NumberStyles.HexNumber);
DateTime epoch = new DateTime(1601, 01, 01);
DateTime myDateTime = epoch.AddMilliseconds(secondsAfterEpoch);
string result = myDateTime.ToString("yyyy-MM-dd");
MessageBox.Show(result);



private void toFloat()
{
    double result = Convert.ToInt64("41D258BC7C800000", 16);
    string resultAux = result.ToString("0000.00");
    MessageBox.Show(resultAux);

    // string hexString = "41D258BC7C800000";
    //Int64 IntRep = Int64.Parse(hexString, NumberStyles.AllowHexSpecifier);
    // byte[] floatVals = BitConverter.GetBytes(IntRep);
    //double f = BitConverter.ToSingle(floatVals, 0);
    //string resultAux = IntRep.ToString();
    //   string resultAux = result.ToString("0000.00");
    // MessageBox.Show(resultAux);
}


     private void toInt()
     {
         string hex = "1400";
         int result = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
         string resultAux = result.ToString();
         MessageBox.Show(resultAux);//se convierte a string solo para imprimirlo 
     }



private void toString()
{
    string hex = "45006E0072006900710075006500";
    string result = String.Empty;
    for (int i = 0; i < hex.Length - 1; i += 2)
    {
        string auxSplit = hex.Substring(i, 2);
        int auxInt = Convert.ToInt32(auxSplit, 16);
        if (auxInt > 31 && auxInt < 127)
            result += Char.ConvertFromUtf32(auxInt);
    }
}


   private void toBigInt()
   {
       long result = Convert.ToInt64("E94C827CEB", 16);

   }



     private void toTinyInt()
     {
         byte result = byte.Parse("0C", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

     }


    private void toDecimal()
    {
        double result = Convert.ToInt64("E2A4", 16);
        string resultAux = result.ToString("0000.00");
        MessageBox.Show(resultAux);
    }


    private void toBit()
    {
        string hexString = "01";
        Int32 IntRep = Int32.Parse(hexString, NumberStyles.AllowHexSpecifier);
        string resultAux = IntRep.ToString();
        MessageBox.Show(resultAux);
    }


private void toMoney()
{
    double result = Convert.ToInt64("0000000000BD4A98", 16);
    string resultAux = result.ToString("0000.00");
    MessageBox.Show(resultAux);
}



private void toFloat()
{
    double result = Convert.ToInt64("41D258BC7C800000", 16);
    string resultAux = result.ToString("0000.00");
    MessageBox.Show(resultAux);
}


private void toChar()
{
    string hex = "45";
    string result = String.Empty;
    int auxInt = Convert.ToInt32(hex, 16);
    if (auxInt > 31 && auxInt < 127)
        result += Char.ConvertFromUtf32(auxInt);
}


  private void toReal()
  {
      string hexString = "0BD31C01";
      Int32 IntRep = Int32.Parse(hexString, NumberStyles.AllowHexSpecifier);
      byte[] floatVals = BitConverter.GetBytes(IntRep);
      float f = BitConverter.ToSingle(floatVals, 0);
      string resultAux = IntRep.ToString();
      MessageBox.Show(resultAux);
  }

private void toBinary()
{
    string hexString = "D3";
    string result = Convert.ToString(Convert.ToInt64(hexString, 16), 2).PadLeft(4, '0');
    MessageBox.Show(result);
}


private void toDateTime() {
    string hex = "B95501EFAA";
    string result = String.Empty;
    for (int i = 0; i < hex.Length-1; i+=2)
    {
        string auxSub = hex.Substring(i, 2);
        int auxInt = int.Parse(auxSub, NumberStyles.AllowHexSpecifier);
        string auxConvert = Char.ConvertFromUtf32(auxInt);
        result += auxConvert;
    }
    CultureInfo provider = CultureInfo.InvariantCulture;
    DateTime var =DateTime.ParseExact(result, "yyyyMMddHHmmss", provider);
    string result2 = var.ToString();
    MessageBox.Show(result2);
}


private void toDateTime()
{
    string hexDate = "5F59D207";
    long secondsAfterEpoch = Int64.Parse(hexDate, System.Globalization.NumberStyles.HexNumber);
    DateTime epoch = new DateTime(1970, 01, 01);
    DateTime myDateTime = epoch.AddSeconds(secondsAfterEpoch);
    string result = myDateTime.ToString("yyyy-MM-dd hh:mm:ss.mmm");
    MessageBox.Show(result);
}



   private void toSmallDateTime()
   {
       string hexDate = "5F59D207";
       long secondsAfterEpoch = Int64.Parse(hexDate, System.Globalization.NumberStyles.HexNumber);
       DateTime epoch = new DateTime(1970, 01, 01);
       DateTime myDateTime = epoch.AddSeconds(secondsAfterEpoch);
       string result = myDateTime.ToString("yyyy-MM-dd hh:mm:ss");
       MessageBox.Show(result);
   }*/
