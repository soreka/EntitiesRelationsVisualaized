using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
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
        HashSet<string> existingTables = new HashSet<string>();
        HashSet<string> existingNodes = new HashSet<string>();
        Dictionary<string, TableRelationship> tableRelationships = new Dictionary<string, TableRelationship>();



        var graph = new Graph("classDiagramGraph");

        string relationsQuery = @"

                WITH JunctionTables AS (
                    -- Identify potential junction tables (Many-to-Many)
                    SELECT
                        fk.parent_object_id AS JunctionTableID,
                        COUNT(fk.object_id) AS ForeignKeyCount
                    FROM
                        sys.foreign_keys fk
                    GROUP BY
                        fk.parent_object_id
                    HAVING
                        COUNT(fk.object_id) = 2  -- Junction tables typically have exactly 2 foreign keys
                )
                SELECT 
                    fk.name AS ForeignKeyName,
                    tp.name AS ParentTable,
                    tr.name AS ReferencedTable,
                    pc.name AS ParentColumn,
                    rc.name AS ReferencedColumn,
                    CASE
                        -- Check for Many-to-Many relationships
                        WHEN jt.JunctionTableID IS NOT NULL THEN 'many-many'

                        -- Check for One-to-One relationships
                        WHEN (
                            SELECT COUNT(*) 
                            FROM sys.indexes i
                            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                            WHERE ic.column_id = fkc.parent_column_id 
                            AND i.is_unique = 1 
                            AND ic.object_id = tp.object_id) > 0 
                            AND 
                            (SELECT COUNT(*) 
                            FROM sys.indexes i
                            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                            WHERE ic.column_id = fkc.referenced_column_id 
                            AND i.is_unique = 1 
                            AND ic.object_id = tr.object_id) > 0 
                        THEN '1-1'

                        -- Check for One-to-Many relationships
                        WHEN 
                            (SELECT COUNT(*) 
                            FROM sys.indexes i
                            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                            WHERE ic.column_id = fkc.parent_column_id 
                            AND i.is_unique = 1 
                            AND ic.object_id = tp.object_id) = 0 
                            AND 
                            (SELECT COUNT(*) 
                            FROM sys.indexes i
                            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                            WHERE ic.column_id = fkc.referenced_column_id 
                            AND i.is_unique = 1 
                            AND ic.object_id = tr.object_id) > 0
                        THEN 'many-1'

                        -- Default to One-to-Many relationships
                        ELSE '1-many'
                    END AS RelationshipType

                FROM 
                    sys.foreign_keys AS fk
                INNER JOIN 
                    sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN 
                    sys.tables AS tp ON fk.parent_object_id = tp.object_id
                INNER JOIN 
                    sys.tables AS tr ON fk.referenced_object_id = tr.object_id
                INNER JOIN 
                    sys.columns AS pc ON tp.object_id = pc.object_id AND pc.column_id = fkc.parent_column_id
                INNER JOIN 
                    sys.columns AS rc ON tr.object_id = rc.object_id AND rc.column_id = fkc.referenced_column_id
                LEFT JOIN 
                    JunctionTables jt ON fk.parent_object_id = jt.JunctionTableID
                ORDER BY 
                    ParentTable, ReferencedTable, RelationshipType;

                ";

        using (var reader = DatabaseManager.ExecuteQuery(connectionString, relationsQuery))
        {
            while (reader.Read())
            {
                string parentTable = reader["ParentTable"].ToString();
                string referencedTable = reader["ReferencedTable"].ToString();
                string relationship = reader["RelationshipType"].ToString();
                existingTables.Add(referencedTable);
                existingTables.Add(parentTable);
                if (!tableRelationships.ContainsKey(parentTable))
                {
                    TableRelationship relation = new TableRelationship(parentTable, referencedTable, relationship);
                    tableRelationships.Add(parentTable, relation);
                }
                //CreateNode(graph, parentTable, existingNodes);
                //CreateNode(graph, referencedTable, existingNodes);
                //CreateEdge(graph, existingEdges, parentTable, referencedTable,
                //relationship);
            }
        }

        string primaryKeysQuery = @"
                    SELECT 
                        kcu.TABLE_NAME,
                        kcu.COLUMN_NAME AS PrimaryKeyColumn,
                        tc.CONSTRAINT_NAME AS PrimaryKeyName
                    FROM 
                        INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
                    JOIN 
                        INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
                        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                        AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                        AND tc.TABLE_NAME = kcu.TABLE_NAME
                    WHERE 
                        tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ORDER BY 
                        kcu.TABLE_NAME, kcu.ORDINAL_POSITION;
                        ";
        using (var reader = DatabaseManager.ExecuteQuery(connectionString, primaryKeysQuery))
        {
            while (reader.Read())
            {
                string parentTable = reader["TABLE_NAME"].ToString();
                string key = reader["PrimaryKeyColumn"].ToString();
                if (tableRelationships.ContainsKey(parentTable))
                {
                    tableRelationships[parentTable].Keys.Append(key);
                }
            }
        }

        foreach (var tableRelationship in tableRelationships.Values)
        {
            CreateNode(graph, tableRelationship.ParentTable, existingNodes);
            CreateNode(graph, tableRelationship.ParentTable, existingNodes);
            CreateEdge(graph, existingEdges, tableRelationship.ParentTable, tableRelationship.ReferencedTable, tableRelationship.Relationship);

        }

        return graph;

    }

    public class TableRelationship
    {
        // Properties
        public string ParentTable { get; set; }
        public string ReferencedTable { get; set; }
        public string Relationship { get; set; }
        public string[] Keys { get; set; }

        // Constructor
        public TableRelationship(string parentTable, string referencedTable, string relationship, string[] keys = null)
        {
            ParentTable = parentTable;
            ReferencedTable = referencedTable;
            Relationship = relationship;
            Keys = keys ?? new string[0]; // Initialize keys as an empty array if null is passed
        }
    }


    private static Tuple<string, string> CreateEdgeKey(string source, string target)
    {
        return source.CompareTo(target) < 0 ? Tuple.Create(source, target) : Tuple.Create(target, source);
    }


    private static void CreateEdge(Graph graph, HashSet<Tuple<string, string>> existingEdges,
        string source, string target, string relationship)
    {

        var edgeKey = CreateEdgeKey(source, target);
        if (!existingEdges.Contains(edgeKey))
        {
            Microsoft.Msagl.Drawing.Edge e = graph.AddEdge(source, target);
            existingEdges.Add(edgeKey);
            e.Attr.Color = Microsoft.Msagl.Drawing.Color.PowderBlue;
            e.Attr.ArrowheadAtSource = ArrowStyle.Diamond; // Diamond at source for 'consists of'
            e.Attr.ArrowheadAtTarget = ArrowStyle.None;
            e.UserData = relationship;
        }
    }

    private static void getNodeKeys()
    {
        var keys = new List<string>();
    }





    private static Subgraph CreateClusterWithWideNodes(string clusterName, string[] nodeNames, Graph graph)
    {
        var cluster = new Subgraph(clusterName)
        {
            LabelText = clusterName,
            Attr = { FillColor = Microsoft.Msagl.Drawing.Color.Ivory },

        };
        cluster.Attr.LineWidth = 0;
        cluster.Label.FontColor = Microsoft.Msagl.Drawing.Color.CornflowerBlue;
        cluster.Attr.Shape = Shape.Box;
        cluster.Attr.LabelWidthToHeightRatio = 0;
        foreach (var nodeName in nodeNames)
        {
            var node = graph.AddNode($"{nodeName}_{clusterName}");
            node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.CornflowerBlue;
            node.Label.FontColor = Microsoft.Msagl.Drawing.Color.Ivory;
            node.LabelText = $"  {nodeName}  ";

            cluster.AddNode(node);
        }

        return cluster;
    }

    private static void CreateNode(Graph graph, string data, HashSet<string> existingNodes)
    {

        //var labelBuilder = new StringBuilder();
        //labelBuilder.Append($"{data+1} \n");
        //labelBuilder.Append($"{data+2}\n");
        //labelBuilder.Append($"{data+3}\n");
        //var node2 = new Node(data)
        //{
        //    LabelText = labelBuilder.ToString()
        //};
        var node = new Microsoft.Msagl.Drawing.Node(data);
        var keyNode = new Microsoft.Msagl.Drawing.Node("keyFor:" + data);

        //var node = graph.AddNode(data);
        //node.LabelText = labelBuilder.ToString();
        node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.CornflowerBlue; // #1f497d
        node.Label.FontColor = Microsoft.Msagl.Drawing.Color.Ivory; // #ffffef
        node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
        graph.AddNode(node);
    }

}

public static class UIManager
{
    private static System.Windows.Forms.Label edgeInfoLabel;

    public static void ShowGraph(Graph graph)
    {
        GraphWriter graphWriter = new GraphWriter();
        var viewer = new GViewer
        {
            Graph = graph,
            Dock = DockStyle.Fill
        };
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












//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Windows.Forms;
//using Microsoft.Msagl.Core.Geometry.Curves;
//using Microsoft.Msagl.Drawing;
//using Microsoft.Msagl.GraphViewerGdi;
//using Microsoft.Msagl.Layout.Layered;
//using P2 = Microsoft.Msagl.Core.Geometry.Point;
//using Color = System.Drawing.Color;
//using GeomNode = Microsoft.Msagl.Core.Layout.Node;
//using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;
//using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
//using DrawingNode = Microsoft.Msagl.Drawing.Node;
//using Microsoft.Msagl.Layout.MDS;
//using Microsoft.Msagl.Prototype.Ranking;
//using Microsoft.Msagl.Layout.Incremental;
//namespace NodesWithTables
//{
//    public class Program
//    {
//        [STAThread] // Required for Windows Forms applications
//        static void Main()
//        {
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            Application.Run(new Form1());
//        }
//    }
//    public partial class Form1 : Form
//    {
//        GViewer viewer = new GViewer();

//        public Form1()
//        {
//            // Removed InitializeComponent and DisplayGeometryGraph.SetShowFunctions()
//            SuspendLayout();
//            this.Controls.Add(viewer);
//            viewer.Dock = DockStyle.Fill;
//            ResumeLayout();
//            viewer.LayoutAlgorithmSettingsButtonVisible = false;
//            InitGraph();
//        }

//        private ICurve GetNodeBoundary(Microsoft.Msagl.Drawing.Node node)
//        {
//            // We can define the width and height based on the content of the node
//            var tableNode = (TableNode)node.UserData;
//            double width = 150;
//            double height = 30 * (1 + tableNode.Keys.Length); // Adjust height based on the number of keys

//            return CurveFactory.CreateRectangleWithRoundedCorners(width, height, width * radiusRatio, height * radiusRatio, new P2());
//        }

//        private bool DrawNode(DrawingNode node, object graphics)
//        {
//            Graphics g = (Graphics)graphics;
//            var tableNode = (TableNode)node.UserData;

//            double width = 150;  // Width of the node
//            double height = 20;  // Height of each section

//            // Define the colors for each section
//            Color nameColor = Color.LightBlue;
//            Color keysColor = Color.LightGreen;

//            // Save the current transformation
//            System.Drawing.Drawing2D.Matrix originalTransform = g.Transform.Clone();

//            // Perform the transformation to flip the node back correctly
//            g.TranslateTransform((float)node.GeometryNode.Center.X, (float)node.GeometryNode.Center.Y);
//            g.ScaleTransform(1, -1);  // Flip vertically
//            g.TranslateTransform(-(float)node.GeometryNode.Center.X, -(float)node.GeometryNode.Center.Y);

//            // Define the position and size of the rectangles
//            var nameRect = new RectangleF((float)node.GeometryNode.Center.X - (float)width / 2,
//                                          (float)node.GeometryNode.Center.Y - (float)height / 2,
//                                          (float)width, (float)height);
//            var keysRect = new RectangleF((float)node.GeometryNode.Center.X - (float)width / 2,
//                                          (float)node.GeometryNode.Center.Y + (float)height / 2,
//                                          (float)width, (float)height * tableNode.Keys.Length);

//            // Draw the first rectangle with the TableName
//            using (var brush = new SolidBrush(nameColor))
//            {
//                g.FillRectangle(brush, nameRect);
//            }

//            // Draw the TableName in the first rectangle
//            g.DrawString(tableNode.TableName, new Font("Arial", 10), Brushes.Black, nameRect);

//            // Draw the second rectangle with the Keys
//            using (var brush = new SolidBrush(keysColor))
//            {
//                g.FillRectangle(brush, keysRect);
//            }

//            // Draw the keys in the second rectangle
//            string keysText = string.Join("\n", tableNode.Keys);
//            g.DrawString(keysText, new Font("Arial", 8), Brushes.Black, keysRect);

//            // Restore the original transformation
//            g.Transform = originalTransform;

//            return true; // Returning false would enable the default rendering
//        }


//        private void InitGraph()
//        {
//            Graph drawingGraph = new Graph();

//            // Example TableNodes
//            var tableNode1 = new TableNode("Users", new string[] { "UserID", "UserName", "Email" });
//            var tableNode2 = new TableNode("Orders", new string[] { "OrderID", "OrderDate", "UserID" });
//            var tableNode3 = new TableNode("Products", new string[] { "ProductID", "ProductName", "Price" });
//            var tableNode11 = new TableNode("Users2", new string[] { "UserID", "UserName", "Email" });
//            var tableNode21 = new TableNode("Orders2", new string[] { "OrderID", "OrderDate", "UserID" });
//            var tableNode31 = new TableNode("Products2", new string[] { "ProductID", "ProductName", "Price" });
//            var tableNode112 = new TableNode("Users23", new string[] { "UserID", "UserName", "Email" });
//            var tableNode212 = new TableNode("Orders23", new string[] { "OrderID", "OrderDate", "UserID" });
//            var tableNode312 = new TableNode("Products23", new string[] { "ProductID", "ProductName", "Price" }); 

//            // Creating nodes
//            var node1 = new DrawingNode("Node1") { UserData = tableNode1 };
//            var node2 = new DrawingNode("Node2") { UserData = tableNode2 };
//            var node3 = new DrawingNode("Node3") { UserData = tableNode3 };
//            // Creating nodes
//            var node122 = new DrawingNode("Node21") { UserData = tableNode21 };
//            var node222 = new DrawingNode("Node212") { UserData = tableNode212 };

//            // Adding nodes to the graph
//            drawingGraph.AddNode(node1);
//            drawingGraph.AddNode(node2);
//            drawingGraph.AddNode(node3);
//            drawingGraph.AddNode(node122);
//            drawingGraph.AddNode(node222);

//            // Connect nodes with edges
//            drawingGraph.AddEdge("Node1", "Node2");
//            drawingGraph.AddEdge("Node2", "Node3");
//            drawingGraph.AddEdge("Node21", "Node2");
//            drawingGraph.AddEdge("Node212", "Node3");

//            // Set up custom rendering and boundaries
//            foreach (DrawingNode node in drawingGraph.Nodes)
//            {
//                node.Attr.Shape = Shape.DrawFromGeometry;
//                node.DrawNodeDelegate = new DelegateToOverrideNodeRendering(DrawNode);
//                node.NodeBoundaryDelegate = new DelegateToSetNodeBoundary(GetNodeBoundary);
//            }

//            double width = 150;
//            double height = 30 * (1 + 3);
//            drawingGraph.Attr.LayerSeparation = height / 2;
//            drawingGraph.Attr.NodeSeparation = width / 2;
//            double arrowHeadLenght = (width / 5);
//            foreach (Microsoft.Msagl.Drawing.Edge e in drawingGraph.Edges)
//                e.Attr.ArrowheadLength = (float)arrowHeadLenght;
//            // Set layout settings

//            drawingGraph.LayoutAlgorithmSettings = new MdsLayoutSettings();
//            viewer.Graph = drawingGraph;
//        }

//        private float radiusRatio = 0.3f;
//    }

//    // Define the TableNode class to hold table names and keys
//    public class TableNode
//    {
//        public string TableName { get; set; }
//        public string[] Keys { get; set; }

//        public TableNode(string tableName, string[] keys)
//        {
//            TableName = tableName;
//            Keys = keys;
//        }
//    }
//}
