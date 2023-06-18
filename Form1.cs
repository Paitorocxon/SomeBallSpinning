using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class Form1 : Form
{
    private const int Width = 800;
    private const int Height = 600;

    private Timer timer;
    private float rotationAngleX;
    private float rotationAngleY;
    private float rotationAngleZ;
    private Point3D[] vertices;
    private Face[] faces;

    private Pen wireframePen;
    private Brush sphereBrush;
    private LightSource lightSource;

    public Form1()
    {
        wireframePen = new Pen(Color.Black);
        sphereBrush = new SolidBrush(Color.LightBlue);

        lightSource = new LightSource(new Vector3D(0.5f, -1f, -0.5f), Color.White);

        vertices = CreateSphereVertices(20, 40);
        faces = CreateSphereFaces(vertices, 40);

        this.ClientSize = new Size(Width, Height);
        this.Text = "Sphere View";
        this.DoubleBuffered = true;
        this.Paint += new PaintEventHandler(DrawSphere);

        timer = new Timer();
        timer.Interval = 20;
        timer.Tick += new EventHandler(UpdateRotation);
        timer.Start();
    }
    private int offsetX = 0;
    private int offsetY = 0;
    private float zoom = 0.02f;
    private const float ZoomStep = 10.0f;

    // ...

    private float sphereSize = 0.002f;
    private const float SizeStep = 0.1f;

    // ...

    private void DrawSphere(object sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Clear the background
        graphics.Clear(Color.White);

        // Calculate the center of the window
        int centerX = ClientSize.Width / 2;
        int centerY = ClientSize.Height / 2;

        // Calculate the maximum radius that fits inside the window
        int maxRadius = (int)(Math.Min(centerX, centerY) * sphereSize) - 10;

        // Translate and rotate the graphics context
        graphics.TranslateTransform(centerX, centerY);

        // Rotate the sphere on the spot
        graphics.RotateTransform(rotationAngleX, MatrixOrder.Append);
        graphics.RotateTransform(rotationAngleY, MatrixOrder.Append);
        graphics.RotateTransform(rotationAngleZ, MatrixOrder.Append);

        // Draw the sphere
        foreach (var face in faces)
        {
            PointF[] points = new PointF[face.Vertices.Length];

            for (int i = 0; i < face.Vertices.Length; i++)
            {
                Point3D vertex = face.Vertices[i];
                float x = vertex.X * maxRadius;
                float y = vertex.Y * maxRadius;
                points[i] = new PointF(x, y);
            }

            Vector3D normal = CalculateFaceNormal(face.Vertices[0], face.Vertices[1], face.Vertices[2]);
            float intensity = lightSource.CalculateIntensity(normal);

            float CI = intensity;
            if (float.IsNaN(intensity))
            {
                CI = 0;
            }
            Color faceColor = Color.FromArgb((int)Math.Round(CI * Color.LightBlue.R),
                                             (int)Math.Round(CI * Color.LightBlue.G),
                                             (int)Math.Round(CI * Color.LightBlue.B));

            Brush faceBrush = new SolidBrush(faceColor);
            graphics.FillPolygon(faceBrush, points);
            graphics.DrawPolygon(wireframePen, points);
        }
    }

    private void Form1_MouseWheel(object sender, MouseEventArgs e)
    {
        if (e.Delta > 0)
        {
            // Zoom in
            zoom += ZoomStep;
        }
        else if (e.Delta < 0)
        {
            // Zoom out
            zoom -= ZoomStep;
            if (zoom < 0.1f)
            {
                zoom = 0.1f;
            }
        }

        Invalidate();
    }




    // Event handler for the Form's KeyDown event
    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        // Adjust the offset based on arrow key input
        int offsetStep = 10;

        Console.WriteLine("ZOOM:" + zoom.ToString());
        switch (e.KeyCode)
        {
            case Keys.Left:
                offsetX -= offsetStep;
                break;
            case Keys.Right:
                offsetX += offsetStep;
                break;
            case Keys.Up:
                offsetY -= offsetStep;
                break;
            case Keys.Down:
                offsetY += offsetStep;
                break;
            case Keys.NumPad0:
                zoom += ZoomStep;
                break;
            case Keys.NumPad1:
                zoom -= ZoomStep;
                break;

        }

        Invalidate();
    }


    private void UpdateRotation(object sender, EventArgs e)
    {
        rotationAngleX += 2.2f;
        rotationAngleY -= 0.5f;
        rotationAngleZ += 0.3f;

        if (rotationAngleX >= 360)
        {
            rotationAngleX -= 360;
        }

        if (rotationAngleY >= 360)
        {
            rotationAngleY -= 360;
        }

        if (rotationAngleZ >= 360)
        {
            rotationAngleZ -= 360;
        }

        Invalidate();
    }

    private Point3D[] CreateSphereVertices(int radius, int divisions)
    {
        int numVertices = (divisions - 1) * divisions + 2;
        Point3D[] vertices = new Point3D[numVertices];

        float phiStep = (float)Math.PI / divisions;
        float thetaStep = 2 * (float)Math.PI / divisions;

        int vertexIndex = 0;

        // North pole
        vertices[vertexIndex++] = new Point3D(0, radius, 0);

        // Middle vertices
        for (int phiIndex = 1; phiIndex < divisions; phiIndex++)
        {
            float phi = phiIndex * phiStep;
            for (int thetaIndex = 0; thetaIndex < divisions; thetaIndex++)
            {
                float theta = thetaIndex * thetaStep;

                float x = (float)(radius * Math.Sin(phi) * Math.Cos(theta));
                float y = (float)(radius * Math.Cos(phi));
                float z = (float)(radius * Math.Sin(phi) * Math.Sin(theta));

                vertices[vertexIndex++] = new Point3D(x, y, z);
            }
        }

        // South pole
        vertices[vertexIndex] = new Point3D(0, -radius, 0);

        return vertices;
    }

    private Face[] CreateSphereFaces(Point3D[] vertices, int divisions)
    {
        int numFaces = 2 * divisions * (divisions - 1);
        Face[] faces = new Face[numFaces];

        int faceIndex = 0;

        // Top faces
        for (int thetaIndex = 0; thetaIndex < divisions; thetaIndex++)
        {
            int vertexIndex1 = 0;
            int vertexIndex2 = thetaIndex + 1;
            int vertexIndex3 = (thetaIndex + 1) % divisions + 1;

            faces[faceIndex++] = new Face(vertices[vertexIndex1], vertices[vertexIndex2], vertices[vertexIndex3]);
        }

        // Middle faces
        for (int phiIndex = 1; phiIndex < divisions - 1; phiIndex++)
        {
            for (int thetaIndex = 0; thetaIndex < divisions; thetaIndex++)
            {
                int vertexIndex1 = (phiIndex - 1) * divisions + thetaIndex + 1;
                int vertexIndex2 = (phiIndex - 1) * divisions + (thetaIndex + 1) % divisions + 1;
                int vertexIndex3 = phiIndex * divisions + (thetaIndex + 1) % divisions + 1;

                faces[faceIndex++] = new Face(vertices[vertexIndex1], vertices[vertexIndex2], vertices[vertexIndex3]);

                vertexIndex1 = phiIndex * divisions + (thetaIndex + 1) % divisions + 1;
                vertexIndex2 = phiIndex * divisions + thetaIndex + 1;
                vertexIndex3 = (phiIndex - 1) * divisions + thetaIndex + 1;

                faces[faceIndex++] = new Face(vertices[vertexIndex1], vertices[vertexIndex2], vertices[vertexIndex3]);
            }
        }

        // Bottom faces
        int lastIndex = vertices.Length - 1;
        for (int thetaIndex = 0; thetaIndex < divisions; thetaIndex++)
        {
            int vertexIndex1 = lastIndex;
            int vertexIndex2 = lastIndex - (thetaIndex + 1) % divisions;
            int vertexIndex3 = lastIndex - thetaIndex;

            faces[faceIndex++] = new Face(vertices[vertexIndex1], vertices[vertexIndex2], vertices[vertexIndex3]);
        }

        return faces;
    }

    private Vector3D CalculateFaceNormal(Point3D vertex1, Point3D vertex2, Point3D vertex3)
    {
        Vector3D vector1 = new Vector3D(vertex2.X - vertex1.X, vertex2.Y - vertex1.Y, vertex2.Z - vertex1.Z);
        Vector3D vector2 = new Vector3D(vertex3.X - vertex1.X, vertex3.Y - vertex1.Y, vertex3.Z - vertex1.Z);
        return vector1.CrossProduct(vector2).Normalize();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Do nothing to prevent background painting
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            wireframePen.Dispose();
            sphereBrush.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "Form1";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.ResumeLayout(false);

    }
}

public class Point3D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Point3D(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public class Vector3D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3D(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3D CrossProduct(Vector3D vector)
    {
        float x = Y * vector.Z - Z * vector.Y;
        float y = Z * vector.X - X * vector.Z;
        float z = X * vector.Y - Y * vector.X;

        return new Vector3D(x, y, z);
    }

    public float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public Vector3D Normalize()
    {
        float length = Length();
        return new Vector3D(X / length, Y / length, Z / length);
    }
}

public class Face
{
    public Point3D[] Vertices { get; set; }

    public Face(params Point3D[] vertices)
    {
        Vertices = vertices;
    }
}

public class LightSource
{
    private Vector3D direction;
    private Color color;

    public LightSource(Vector3D direction, Color color)
    {
        this.direction = direction.Normalize();
        this.color = color;
    }

    public float CalculateIntensity(Vector3D normal)
    {
        float dotProduct = direction.X * normal.X + direction.Y * normal.Y + direction.Z * normal.Z;
        return Math.Max(dotProduct, 0f);
    }
}
