using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Color = Microsoft.Msagl.Drawing.Color;

namespace entitiesRelations;
    

    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source= (localdb)\\myWorkSpace ;Initial Catalog=AdventureWorks2016; User ID=soreka; Password=soreka123";

            // Create and fetch the graph
            var graph = GraphManager.BuildGraph(connectionString);

            // Setup the UI
            UIManager.ShowGraph(graph);
        }
        }
    public static class DatabaseManager
    {
        public static SqlDataReader ExecuteQuery(string connectionString, string query)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand(query, connection);
            return command.ExecuteReader();
        }
    }
    public static class GraphManager
    {
        public static Graph BuildGraph(string connectionString)
        {
            HashSet<Tuple<string, string>> existingEdges = new HashSet<Tuple<string, string>>();
            
            var graph = new Graph("databaseSchema");

            string queryOneToMany = @"
                  SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    tr.name AS ReferencedTable,
    pc.name AS ParentColumn,
    rc.name AS ReferencedColumn,
    CASE
        WHEN (
            SELECT COUNT(*) 
              FROM sys.indexes i
              INNER JOIN 
              sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
              WHERE ic.column_id = fkc.parent_column_id AND i.is_unique = 1 AND ic.object_id = tp.object_id) > 0 
             AND 
             (SELECT COUNT(*) 
              FROM sys.indexes i
              INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
              WHERE ic.column_id = fkc.referenced_column_id AND i.is_unique = 1 AND ic.object_id = tr.object_id) > 0 
        THEN '1-1'
        WHEN 
            (SELECT COUNT(*) 
             FROM sys.indexes i
             INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
             WHERE ic.column_id = fkc.parent_column_id AND i.is_unique = 1 AND ic.object_id = tp.object_id) = 0 
             AND 
             (SELECT COUNT(*) 
              FROM sys.indexes i
              INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
              WHERE ic.column_id = fkc.referenced_column_id AND i.is_unique = 1 AND ic.object_id = tr.object_id) > 0
        THEN 'many-1'
        ELSE '1-many'
    END AS RelationshipType

FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables AS tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables AS tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.columns AS pc ON tp.object_id = pc.object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.columns AS rc ON tr.object_id = rc.object_id AND rc.column_id = fkc.referenced_column_id;";

            using (var reader = DatabaseManager.ExecuteQuery(connectionString, queryOneToMany))
            {
                while (reader.Read())
                {
                    string parentTable = reader["ParentTable"].ToString();
                    string referencedTable = reader["ReferencedTable"].ToString();
                    string relationship = reader["RelationshipType"].ToString();
                    CreateNode(graph, parentTable);
                    CreateNode(graph, referencedTable);
                    CreateEdge(graph, existingEdges, parentTable, referencedTable,
                    relationship);
                //Edge e = graph.AddEdge(parentTable, referencedTable );
                    //e.LabelText = relationship;
                    //e.Label.IsVisible = false;
                    //e.Label.FontColor = Microsoft.Msagl.Drawing.Color.Red;
                    //e.Attr.Color = Microsoft.Msagl.Drawing.Color.PowderBlue;
                    //e.Attr.ArrowheadAtSource = ArrowStyle.Diamond; // Diamond at source for 'consists of'
                    //e.Attr.ArrowheadAtTarget = ArrowStyle.None;
                    //e.UserData = relationship;
                }
            }
            string queryManyToMany = @"
                SELECT
                    junction.name AS JunctionTable,
                    tp.name AS ParentTable,
                    tr.name AS ReferencedTable
                FROM
                    sys.foreign_keys AS fk1
                INNER JOIN
                    sys.foreign_key_columns AS fkc1 ON fk1.object_id = fkc1.constraint_object_id
                INNER JOIN
                    sys.tables AS junction ON fk1.parent_object_id = junction.object_id
                INNER JOIN
                    sys.tables AS tp ON fk1.referenced_object_id = tp.object_id
                INNER JOIN
                    sys.foreign_keys AS fk2 ON fk2.parent_object_id = junction.object_id AND fk2.object_id != fk1.object_id
                INNER JOIN
                    sys.foreign_key_columns AS fkc2 ON fk2.object_id = fkc2.constraint_object_id
                INNER JOIN
                    sys.tables AS tr ON fk2.referenced_object_id = tr.object_id;
              ";
                using (var reader = DatabaseManager.ExecuteQuery(connectionString, queryManyToMany))
                {
                while (reader.Read())
                {
                    string parentTable = reader["ParentTable"].ToString();
                    string referencedTable = reader["ReferencedTable"].ToString();
                    string relationship = "many-many";
                    CreateNode(graph, parentTable);
                    CreateNode(graph, referencedTable);
                    CreateEdge(graph, existingEdges, parentTable, referencedTable, 
                        relationship);
                    //Edge e = graph.AddEdge(parentTable, referencedTable);
                    //e.Attr.Color = Microsoft.Msagl.Drawing.Color.PowderBlue;
                    //e.Attr.ArrowheadAtSource = ArrowStyle.Diamond; // Diamond at source for 'consists of'
                    //e.Attr.ArrowheadAtTarget = ArrowStyle.None;
                    //e.UserData = relationship;
                }
                }
        return graph;

        }
    
        private static  Tuple<string,string>  CreateEdgeKey(string source,string target)
        {
            return source.CompareTo(target) < 0 ? Tuple.Create(source, target) : Tuple.Create(target,source);
        }
        private static void CreateEdge(Graph graph,HashSet<Tuple<string,string>> existingEdges,
            string source,string target,string relationship)
        {
            
            var edgeKey = CreateEdgeKey(source,target);
            if (!existingEdges.Contains(edgeKey)) {
                Edge e = graph.AddEdge(source, target);
                existingEdges.Add(edgeKey);
                e.Attr.Color = Microsoft.Msagl.Drawing.Color.PowderBlue;
                e.Attr.ArrowheadAtSource = ArrowStyle.Diamond; // Diamond at source for 'consists of'
                e.Attr.ArrowheadAtTarget = ArrowStyle.None;
                e.UserData = relationship;
        }
        }
        private static void CreateNode(Graph graph, string data)
        {
            var Node = graph.AddNode(data);
            Node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.CornflowerBlue; // #1f497d
            Node.Label.FontColor = Microsoft.Msagl.Drawing.Color.Ivory; // #ffffef
            Node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
        }
        

    }

public static class UIManager
{
    private static System.Windows.Forms.Label edgeInfoLabel;

    public static void ShowGraph(Graph graph)
    {
        var viewer = new GViewer { Graph = graph };
        viewer.Dock = DockStyle.Fill;
        viewer.MouseMove += new MouseEventHandler(gViewer_MouseMove);

        var form = new Form
        {
            Text = "Database Schema Visualization",
            WindowState = FormWindowState.Maximized
        };

        edgeInfoLabel = new System.Windows.Forms.Label
        {
            AutoSize = true,
            BackColor = System.Drawing.Color.AntiqueWhite,
            ForeColor = System.Drawing.Color.CornflowerBlue,
            //BorderStyle = BorderStyle.FixedSingle,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(5),
            Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold),
            Visible = false
        };

        form.Controls.Add(edgeInfoLabel);
        form.Controls.Add(viewer);

        Application.Run(form);
    }

    private static void gViewer_MouseMove(object sender, MouseEventArgs e)
    {
        GViewer viewer = sender as GViewer;
        var objectUnderMouseCursor = viewer.GetObjectAt(e.Location);

        if (objectUnderMouseCursor is DNode node1)
        {
            Microsoft.Msagl.Drawing.Node drawingNode = node1.DrawingNode;
        }

        //if (objectUnderMouseCursor is DNode node)
        //{
        //    MessageBox.Show($"{node.GetType().Name}");
        //}

       
        if (objectUnderMouseCursor is DEdge edge)
        {

            Microsoft.Msagl.Drawing.Edge drawingEdge = edge.DrawingEdge;
            edgeInfoLabel.Text = drawingEdge.UserData.ToString();
            edgeInfoLabel.Location = e.Location;
            edgeInfoLabel.Visible = true;

        }
        else
        {
            edgeInfoLabel.Visible = false;
        }
    }
}

