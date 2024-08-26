using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Color = Microsoft.Msagl.Drawing.Color;

class Program
{
    private static System.Windows.Forms.Label edgeInfoLabel;

    static void Main(string[] args)
    {
        
        string connectionString = "Data Source= (localdb)\\myWorkSpace ;Initial Catalog=coapp; User ID=soreka; Password=soreka123";
        var graph = new Graph("databaseSchema");

        // Fetch the schema and relationships
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            //string query = @"
            //    SELECT 
            //        fk.name AS ForeignKeyName,
            //        tp.name AS ParentTable,
            //        cp.name AS ParentColumn,
            //        tr.name AS ReferencedTable,
            //        cr.name AS ReferencedColumn
            //    FROM 
            //        sys.foreign_keys AS fk
            //    INNER JOIN 
            //        sys.tables AS tp ON fk.parent_object_id = tp.object_id
            //    INNER JOIN 
            //        sys.tables AS tr ON fk.referenced_object_id = tr.object_id
            //    INNER JOIN 
            //        sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
            //    INNER JOIN 
            //        sys.columns AS cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
            //    INNER JOIN 
            //        sys.columns AS cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id;
            //";

            string query = @"
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
                    THEN tp.name + ' can have only one ' + tr.name
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
                    THEN tp.name + ' is on of many in ' + tr.name
                    ELSE tp.name + ' can have multiple  ' + tr.name
                END AS RelationshipType

                From sys.foreign_keys AS fk
                INNER JOIN
                sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN 
                sys.tables AS tp ON  fk.parent_object_id = tp.object_id
                INNER JOIN
                sys.tables AS tr ON fk.referenced_object_id = tr.object_id
                INNER JOIN 
                sys.columns AS pc ON tp.object_id = pc.object_id AND pc.column_id = fkc.parent_column_id
                INNER JOIN
                sys.columns AS rc ON tr.object_id = rc.object_id AND rc.column_id = fkc.parent_column_id


             ";

            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string parentTable = reader["ParentTable"].ToString();
                        string referencedTable = reader["ReferencedTable"].ToString();
                        string relationship = reader["RelationshipType"].ToString();
                        graph.AddNode(parentTable);
                        graph.AddNode(referencedTable);
                        Edge e = graph.AddEdge(parentTable, referencedTable);
                        e.LabelText = relationship;
                        e.Label.FontColor = Color.Red;
                        e.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                        //e.LabelText = relationship;
                    }
                }
            }
        }

        // Render the graph
        var viewer = new GViewer { Graph = graph };
        viewer.Dock = DockStyle.Fill;
        viewer.MouseMove += (sender, e) =>
        {
            var obj = viewer.ObjectUnderMouseCursor;

            if (obj is Microsoft.Msagl.Core.Layout.Edge dedge)
            {
                // Find the corresponding graphical edge
                Edge edge = null;
                foreach (var gEdge in viewer.Graph.Edges)
                {
                    if ((gEdge.SourceNode.Id == dedge.SourceNode.Id && gEdge.TargetNode.Id == dedge.TargetNode.Id) ||
                        (gEdge.SourceNode.Id == dedge.TargetNode.Id && gEdge.TargetNode.Id == dedge.SourceNode.Id))
                    {
                        edge = gEdge;
                        break;
                    }
                }

                if (edge != null)
                {
                    // Change color or other properties to indicate hover
                    edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Blue;

                    // Optionally, change the thickness or other attributes
                    edge.Attr.LineWidth = 3.0;

                    // Refresh the viewer to apply changes
                    viewer.Invalidate();
                }
            }
            else
            {
                // Reset all edges to their default state when not hovering
                foreach (var gEdge in viewer.Graph.Edges)
                {
                    gEdge.Attr.Color = Microsoft.Msagl.Drawing.Color.Black; // Or whatever the default color is
                    gEdge.Attr.LineWidth = 1.0;
                }

                viewer.Invalidate();
            }
        };
        //viewer.Click += (sender, e) =>
        //{
        //    var obj = viewer.ObjectUnderMouseCursor;

        //    if (obj == null)
        //    {
        //        MessageBox.Show("No object detected under the mouse cursor.");
        //    }
        //    else if (obj is DEdge edge)
        //    {

        //        //MessageBox.Show($"Edge {edge.Source} -> {edge.Target} clicked");
        //        //// Toggle edge color
        //        //edge.Attr.Color = edge.Attr.Color == Microsoft.Msagl.Drawing.Color.Red
        //        //    ? Microsoft.Msagl.Drawing.Color.Black
        //        //    : Microsoft.Msagl.Drawing.Color.Red;

        //        //// Toggle label visibility
        //        //edge.Label.FontColor = edge.Label.FontColor == Color.Transparent
        //        //    ? Color.Black
        //        //    : Color.Transparent;

        //        // Refresh the viewer to apply changes
        //        viewer.Invalidate();
        //    }
        //    else if (obj is Node node)
        //    {
        //        MessageBox.Show($"Node {node.LabelText} clicked");
        //    }
        //    else
        //    {
        //        MessageBox.Show($"Detected an object of type: {obj.GetType().Name}");
        //    }
        //};

        //        // Create a Windows Forms form to show the graph
        //        var form = new Form
        //        {
        //            Text = "Database Schema Visualization",
        //            WindowState = FormWindowState.Maximized
        //        };

        //        // Create the Label control to display edge information
        //        edgeInfoLabel = new System.Windows.Forms.Label
        //        {
        //            AutoSize = true,
        //            BackColor = System.Drawing.Color.LightYellow,
        //            BorderStyle = BorderStyle.FixedSingle,
        //            Visible = false
        //        };
        //        form.Controls.Add(edgeInfoLabel);

        //        form.Controls.Add(viewer);

        //        Application.Run(form);
        //}

        //    private static void GraphNode_Click(object sender, EventArgs e)
        //    {
        //        GViewer viewer = sender as GViewer;

        //        if (viewer.SelectedObject is Edge edge)
        //        {
        //            var mousePosition = viewer.PointToClient(Control.MousePosition);

        //            edgeInfoLabel.Text = edge.LabelText ?? "Edge Clicked";
        //            edgeInfoLabel.Location = mousePosition;
        //            edgeInfoLabel.Visible = true;
        //        }
        //    }
    }