// Kinect V2 API
using Microsoft.Kinect;
// .NET-based APIs from Microsoft
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Media;
using System.Windows.Media.Imaging;

namespace NUI3D
{
    public class BodyFrameManager
    {
        // my own contribution
        private KinectSensor sensor;
        private Body[] bodies;

        // the following declarations are borrowed from lecture topic 02
        private FrameDescription colorFrameDescription = null;
        private byte[] colorData = null;
        private WriteableBitmap colorImageBitmap = null;
        private Image colorVideo;

        // displaying game content with WPF
        private Image gameMenu;
        private Image background;
        private Label gameOver;
        private Label restartInfo;
        private TextBlock displayScore;
        
        // for non WPF images, we have to load them first
        private BitmapImage money;
        private BitmapImage bodyShape;
        private void LoadImages()
        {
            money = new BitmapImage(
                new Uri("images/money.png", UriKind.Relative));
            bodyShape = new BitmapImage(
                new Uri("images/green.png", UriKind.Relative));
        }
        public void Init(KinectSensor s, Image wpfImageForDisplay, Image menu, Image bg, Label over, TextBlock score, Label restartLabel , Image video, Boolean toColorSpace = true)
        {
            sensor = s;

            gameMenu = menu;
            background = bg;
            colorVideo = video;

            gameOver = over;
            restartInfo = restartLabel;

            displayScore = score;

            mapToColorSpace = toColorSpace;

            LoadImages();

            if (toColorSpace) // map the skeleton to the color space
            {
                drawingImgWidth = sensor.ColorFrameSource.FrameDescription.Width;
                drawingImgHeight = sensor.ColorFrameSource.FrameDescription.Height;
            } else // map the skeleton to the depth space 
            {
                drawingImgWidth = sensor.DepthFrameSource.FrameDescription.Width;
                drawingImgHeight = sensor.DepthFrameSource.FrameDescription.Height;
            }

            DrawingGroupInit(wpfImageForDisplay);

            BodyFrameReaderInit();

            // to show the color video from the kinect
            ColorFrameReaderInit();

        }
        
        private DrawingGroup drawingGroup;
        private DrawingImage drawingImg;
        private static double drawingImgWidth = 1920, drawingImgHeight = 1080;
        private void DrawingGroupInit(Image wpfImageForDisplay) // called in Window_Loaded 
        {
            drawingGroup = new DrawingGroup();
            drawingImg = new DrawingImage(drawingGroup);
            wpfImageForDisplay.Source = drawingImg;

            // prevent drawing outside of our render area
            drawingGroup.ClipGeometry = new RectangleGeometry(
                                        new Rect(0.0, 0.0, drawingImgWidth, drawingImgHeight));
        }

        void Reset()
        {
            // reset game data
            wealth = 0;
            rotateTop = origin;
            rotateBot = origin;
            laserTop = origin;
            laserBot = origin;
            laserMode = 0;
            laserSize = 1;
            laserSpeed = 8;
            rotateSpeed = 2;
            laserAngle = 0;
            level = 1;

            // reset the game over and restart info 
            // when no human can be detected from the kinect
            int count = 0;
            for (int i = 0; i < bodies.Length; ++i)
            {
                if (bodies[i].IsTracked == false)
                    ++count;
            }
            if (count == 6)
            {
                hitByLaser = false;
                gameOver.Visibility = Visibility.Hidden;
                restartInfo.Visibility = Visibility.Hidden;
            }
        }

        // the following codes are borrowed from T8_PoseMatching
        private Boolean mapToColorSpace = true;
        public Point MapCameraPointToScreenSpace(Body body, JointType jointType)
        {
            Point screenPt = new Point(0, 0);
            if (mapToColorSpace) // to color space 
            {
                ColorSpacePoint pt = sensor.CoordinateMapper.MapCameraPointToColorSpace(
                body.Joints[jointType].Position);
                screenPt.X = pt.X;
                screenPt.Y = pt.Y;
            }
            else // to depth space
            {
                DepthSpacePoint pt = sensor.CoordinateMapper.MapCameraPointToDepthSpace(
                body.Joints[jointType].Position);
                screenPt.X = pt.X;
                screenPt.Y = pt.Y;
            }
            return screenPt;
        }

        // the following codes are borrowed from T8_PoseMatching
        private Rect r_base = new Rect(0, 0, 200, 200);
        private void DrawSkeleton(Body body, DrawingContext dc)
        {
            Point pt3 = MapCameraPointToScreenSpace(
                body, JointType.Head);

            r_base = new Rect(pt3.X, pt3.Y, 200, 200);
            dc.DrawImage(bodyShape, r_base);

        }

        // my own contribution
        private double wealth = 0;
        private Random rand = new Random();

        private Point best;
        private Point avg;
        private Point cheapest;

        private Rect bestDollar;
        private Rect avgDollar;
        private Rect cheapestDollar;

        private SoundPlayer moneySound = new SoundPlayer("./Audio/money_bag.wav");
        private void DrawThreeMoney(DrawingContext dc, double resetPosition)
        {
            // reset the position of money when the laser reaches the end
            if (resetPosition == 1)
            {
                best = new Point(rand.Next((int)drawingImgWidth / 2 - 200, (int)drawingImgWidth / 2 - 150),
                    rand.Next((int)drawingImgHeight / 2 - 200, (int)drawingImgHeight / 2 - 100));
                avg = new Point(rand.Next(0, 300), rand.Next((int)drawingImgHeight / 2 - 200, (int)drawingImgHeight / 2 + 200));
                cheapest = new Point(rand.Next(0, 450), rand.Next((int)drawingImgHeight / 2 - 300, (int)drawingImgHeight / 2 + 300));
            }

            // draw three types of money item
            bestDollar = new Rect(best.X, best.Y, 50, 80);
            avgDollar = new Rect(avg.X, avg.Y, 80, 120);
            cheapestDollar = new Rect(cheapest.X, cheapest.Y, 120, 160);
            dc.DrawImage(money, bestDollar);
            dc.DrawImage(money, avgDollar);
            dc.DrawImage(money, cheapestDollar);

            BodyTouchMoney();
        }
        private void BodyTouchMoney()
        {
            if (r_base.Contains(bestDollar))
            {
                wealth += 3;
                best = new Point(-1000, -1000);
                moneySound.Play();
            }
            if (r_base.Contains(avgDollar))
            {
                wealth += 2;
                avg = new Point(-1000, -1000);
                moneySound.Play();
            }
            if (r_base.Contains(cheapestDollar))
            {
                ++wealth;
                cheapest = new Point(-1000, -1000);
                moneySound.Play();
            }

        }

        // my main contribution
        private double level = 1; // <-- the higher the level, the speed of the laser will become faster
        private Boolean gameStart = false;
        private Boolean hitByLaser = false;
        // laser properties
        private int laserMode = 0;
        private double laserSize = 1;
        private double laserSpeed = 8;
        // displaying straight line laser using two points' positions
        private Point laserTop = new Point(drawingImgWidth / 2, drawingImgHeight / 2);
        private Point laserBot = new Point(drawingImgWidth / 2, drawingImgHeight / 2);
        // displaying rotating laser
        private double rotateSpeed = 2;
        private double laserAngle = 0;
        private Point rotateTop = new Point();
        private Point rotateBot = new Point();
        // to reset the laser to the center
        private Point origin = new Point(drawingImgWidth / 2, drawingImgHeight / 2);

        private SoundPlayer laserSound = new SoundPlayer("./Audio/laser.wav");
        private SoundPlayer moanSound = new SoundPlayer("./Audio/moan.wav");

        // the following codes about line intersect with rectangle
        // are copied from the website https://stackoverflow.com/questions/5514366/how-to-know-if-a-line-intersects-a-rectangle
        // from the user named Wojtpl2 
        private static bool LineIntersectsRect(Point p1, Point p2, Rect r)
        {
            return SegmentIntersectRectangle(r.X, r.Y, r.X + r.Width, r.Y + r.Height, p1.X, p1.Y, p2.X, p2.Y);
        }

        private static bool SegmentIntersectRectangle(
        double rectangleMinX,
        double rectangleMinY,
        double rectangleMaxX,
        double rectangleMaxY,
        double p1X,
        double p1Y,
        double p2X,
        double p2Y)
        {
            // Find min and max X for the segment
            double minX = p1X;
            double maxX = p2X;

            if (p1X > p2X)
            {
                minX = p2X;
                maxX = p1X;
            }

            // Find the intersection of the segment's and rectangle's x-projections
            if (maxX > rectangleMaxX)
            {
                maxX = rectangleMaxX;
            }

            if (minX < rectangleMinX)
            {
                minX = rectangleMinX;
            }

            if (minX > maxX) // If their projections do not intersect return false
            {
                return false;
            }

            // Find corresponding min and max Y for min and max X we found before
            double minY = p1Y;
            double maxY = p2Y;

            double dx = p2X - p1X;

            if (Math.Abs(dx) > 0.0000001)
            {
                double a = (p2Y - p1Y) / dx;
                double b = p1Y - a * p1X;
                minY = a * minX + b;
                maxY = a * maxX + b;
            }

            if (minY > maxY)
            {
                double tmp = maxY;
                maxY = minY;
                minY = tmp;
            }

            // Find the intersection of the segment's and rectangle's y-projections
            if (maxY > rectangleMaxY)
            {
                maxY = rectangleMaxY;
            }

            if (minY < rectangleMinY)
            {
                minY = rectangleMinY;
            }

            if (minY > maxY) // If Y-projections do not intersect return false
            {
                return false;
            }

            return true;
        }

        private void DrawLaser(DrawingContext dc, Body body)
        {
            // using the power of the position from the top or bottom of the laser to calculate whether the laser has reaches the end
            // if so then reset the laser
            if (
            (Math.Pow(Math.Abs(laserBot.Y) + Math.Abs(laserBot.X), 2.0) > (Math.Pow(drawingImgWidth, 2.02) + Math.Pow(drawingImgHeight, 2.02)))
            || (Math.Pow(Math.Abs(laserTop.Y) + Math.Abs(laserTop.X), 2.0) > (Math.Pow(drawingImgWidth, 2.02) + Math.Pow(drawingImgHeight, 2.02)))
            )
            {
                laserSound.Play();
                // randomly select a laser moving pattern
                laserMode = rand.Next(0, 6);
                //reset
                rotateTop = origin;
                rotateBot = origin;
                laserTop = origin;
                laserBot = origin;
                laserAngle = 0;
                laserSize = 1;
                // increase level each time the player dodge the laser
                level += 0.05;
            }

            DrawThreeMoney(dc, laserSize);
            laserSize += 0.15;

            switch (laserMode)
            {
                case 0:
                    {   //vertical
                        laserTop.Y += -laserSpeed * level;
                        laserBot.Y += laserSpeed * level;
                        break;
                    }
                case 1:
                    {   //horizontal
                        laserTop.X += -laserSpeed * level;
                        laserBot.X += laserSpeed * level;
                        break;
                    }
                case 2:
                    {   // diagonal top-right and bottom-left
                        laserTop.X += laserSpeed * level;
                        laserTop.Y += -laserSpeed * level;
                        laserBot.X += -laserSpeed * level;
                        laserBot.Y += laserSpeed * level;
                        break;
                    }
                case 3:
                    {   // diagonal top-left and bottom-right
                        laserTop.X += laserSpeed * level;
                        laserTop.Y += laserSpeed * level;
                        laserBot.X += -laserSpeed * level;
                        laserBot.Y += -laserSpeed * level;
                        break;
                    }
                case 4:
                    {   // clockwise
                        laserAngle += rotateSpeed * level;
                        laserTop.Y += (-laserSpeed + 3) * level;
                        laserBot.Y += (laserSpeed - 3) * level;
                        break;
                    }
                case 5:
                    {   // anti-clockwise
                        laserAngle += -rotateSpeed * level;
                        laserTop.Y += (-laserSpeed + 3) * level;
                        laserBot.Y += (laserSpeed - 3) * level;
                        break;
                    }
                default:break;

            }

            // the following codes about rotation are refer to https://stackoverflow.com/questions/6241740/getting-new-position-of-a-line-after-rotation
            RotateTransform rotation = new RotateTransform(laserAngle, origin.X, origin.Y);
            rotateTop = rotation.Transform(new Point(laserTop.X, laserTop.Y));
            rotateBot = rotation.Transform(new Point(laserBot.X, laserBot.Y));

            // using green, yello or red to indicate the how far the laser is from the player
            // green meaning far away
            // yellow meaning close by
            // red meaning it will hurt the player
            if (
                (laserBot.Y > drawingImgHeight / 2) && (laserBot.Y < (drawingImgHeight / 2 + drawingImgHeight / 2 / 5))
            || (laserBot.X > drawingImgWidth / 2) && (laserBot.X < (drawingImgWidth / 2 + drawingImgWidth / 2 / 5))
            || (laserTop.Y > drawingImgHeight / 2) && (laserTop.Y < (drawingImgHeight / 2 + drawingImgHeight / 2 / 5))
            || (laserTop.X > drawingImgWidth / 2) && (laserTop.X < (drawingImgWidth / 2 + drawingImgWidth / 2 / 5))
            )
            {
                if (laserMode >= 4)
                    dc.DrawLine(new Pen(Brushes.Green, laserSize), rotateTop, rotateBot);
                else dc.DrawLine(new Pen(Brushes.Green, laserSize), laserTop, laserBot);
            }
            else if ((laserBot.Y > (drawingImgHeight / 2 + drawingImgHeight / 2 / 5)) && (laserBot.Y < (drawingImgHeight / 2 + drawingImgHeight / 2 / 5 + drawingImgHeight / 2 / 5))
                || (laserBot.X > (drawingImgWidth / 2 + drawingImgWidth / 2 / 5)) && (laserBot.X < (drawingImgWidth / 2 + drawingImgWidth / 2 / 5 + drawingImgWidth / 2 / 5))
                || (laserTop.Y > (drawingImgHeight / 2 + drawingImgHeight / 2 / 5)) && (laserTop.Y < (drawingImgHeight / 2 + drawingImgHeight / 2 / 5 + drawingImgHeight / 2 / 5))
                || (laserTop.X > (drawingImgWidth / 2 + drawingImgWidth / 2 / 5)) && (laserTop.X < (drawingImgWidth / 2 + drawingImgWidth / 2 / 5 + drawingImgWidth / 2 / 5))
                )
            {
                if (laserMode >= 4)
                    dc.DrawLine(new Pen(Brushes.Yellow, laserSize), rotateTop, rotateBot);
                else dc.DrawLine(new Pen(Brushes.Yellow, laserSize), laserTop, laserBot);
            }
            else
            {
                if (laserMode >= 4)
                    dc.DrawLine(new Pen(Brushes.Red, laserSize), rotateTop, rotateBot);
                else dc.DrawLine(new Pen(Brushes.Red, laserSize), laserTop, laserBot);
            }


            if (laserMode < 4) // check whether the square hit the line
            {
                if (
                (laserBot.Y > (drawingImgHeight / 2 + drawingImgHeight / 2 / 5 + drawingImgHeight / 2 / 5))
                || (laserBot.X > (drawingImgWidth / 2 + drawingImgWidth / 2 / 5 + drawingImgWidth / 2 / 5))
                || (laserTop.Y > (drawingImgHeight / 2 + drawingImgHeight / 2 / 5 + drawingImgHeight / 2 / 5))
                || (laserTop.X > (drawingImgWidth / 2 + drawingImgWidth / 2 / 5 + drawingImgWidth / 2 / 5))
                )
                {
                    if (LineIntersectsRect(laserTop, laserBot, r_base))
                    {
                        moanSound.Play();
                        hitByLaser = true;
                        gameOver.Visibility = Visibility.Visible;
                        restartInfo.Visibility = Visibility.Visible;
                    }
                }
            }
            else if (laserMode >= 4)// check whether the square hit the rotated line
            {
                if (
                (rotateBot.Y > (drawingImgHeight / 2 + drawingImgHeight / 2 / 5 + drawingImgHeight / 2 / 5))
                || (rotateBot.X > (drawingImgWidth / 2 + drawingImgWidth / 2 / 5 + drawingImgWidth / 2 / 5))
                || (rotateTop.Y > (drawingImgHeight / 2 + drawingImgHeight / 2 / 5 + drawingImgHeight / 2 / 5))
                || (rotateTop.X > (drawingImgWidth / 2 + drawingImgWidth / 2 / 5 + drawingImgWidth / 2 / 5))
                )
                {
                    if (LineIntersectsRect(rotateTop, rotateBot, r_base))
                    {
                        moanSound.Play();
                        hitByLaser = true;
                        gameOver.Visibility = Visibility.Visible;
                        restartInfo.Visibility = Visibility.Visible;
                    }
                }
            }

        }

        private void BodyFrameReaderInit()
        {
            BodyFrameReader bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;

            // BodyCount: maximum number of bodies that can be tracked at one time
            bodies = new Body[sensor.BodyFrameSource.BodyCount];
        }
        // the following codes are borrowed from T8_PoseMatching and added my own codes
        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null) return;

                bodyFrame.GetAndRefreshBodyData(bodies);

                if (hitByLaser) Reset();

                if (gameStart && !hitByLaser)
                {
                    using (DrawingContext dc = drawingGroup.Open())
                    {
                        // draw a transparent background to set the render size
                        dc.DrawRectangle(Brushes.Transparent, null,
                                new Rect(0.0, 0.0, drawingImgWidth, drawingImgHeight));

                        foreach (Body body in bodies)
                        {
                            if (body.IsTracked)
                            {
                                // draw a skeleton
                                displayScore.Text = "Score : " + wealth;
                                DrawSkeleton(body, dc);
                                DrawLaser(dc, body);
                            }
                        }
                    }
                }
                else// restart the game when player is detected by the Kinect
                {
                    for (int i = 0; i < bodies.Length; ++i)
                    {
                        if (bodies[i].IsTracked == true)
                        {
                            gameStart = true;
                            gameMenu.Visibility = Visibility.Hidden;
                            background.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        // the following codes are borrowed from lecture topic 02
        private void ColorFrameReaderInit()
        {
            colorFrameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // intermediate storage for receiving frame data from the sensor 
            colorData = new byte[colorFrameDescription.LengthInPixels * colorFrameDescription.BytesPerPixel];

            colorImageBitmap = new WriteableBitmap(
                      colorFrameDescription.Width,
                      colorFrameDescription.Height,
                      96, // dpi-x
                      96, // dpi-y
                      PixelFormats.Bgr32, // pixel format  
                      null);

            colorVideo.Source = colorImageBitmap;

            ColorFrameReader colorFrameReader = sensor.ColorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;

        }

        // the following codes are borrowed from lecture topic 02
        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame == null) return;

                colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);

                colorImageBitmap.WritePixels(
                   new Int32Rect(0, 0,
                   colorFrameDescription.Width, colorFrameDescription.Height), // source rect
                   colorData, // pixel data
                              // stride: width in bytes of a single row of pixel data
                   colorFrameDescription.Width * (int)(colorFrameDescription.BytesPerPixel),
                   0 // offset 
                );
            }

        }

    }
}
