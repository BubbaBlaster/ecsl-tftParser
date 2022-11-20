using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Agora.Forms
{
    /// DLSIcons provides the Schlumberger DLS Icons converted to a true-type font.  The const chars
    /// within the class are the unicode characters of the font.  
    /// <example><code>
    /// label1.Font = new Font( Agora.Forms.DLSIcons.Font, label1.Font.Size );
    /// label1.Text = DLSIcons.ICO_AIRPLANE.ToString();
    /// </code></example>
    public class DLSIcon : UserControl
    {

        [Category("Agora")]
        public eDLSIcon Icon { get; set; } = eDLSIcon.ICO_2D_SEISMIC;

        [Category("Agora")]
        public float IconSize { get; set; } = 16;

        /// Returns a FontFamily containing the DLSIcons
        private FontFamily _font
        {
            get
            {
                if (_pfc == null)
                {
                    _pfc = new PrivateFontCollection();
                    int fontLength = Properties.Resources.SchlumbergerDLSIcons.Length;
                    IntPtr data = Marshal.AllocCoTaskMem(fontLength);
                    Marshal.Copy(Properties.Resources.SchlumbergerDLSIcons, 0, data, fontLength);
                    _pfc.AddMemoryFont(data, fontLength);
                }
                return _pfc.Families[0];
            }
        }
        static PrivateFontCollection? _pfc = null;

        public DLSIcon()
        {
            ForeColor = Colors.LM_White;
            Paint += DLSIcon_Paint;
        }

        Brush? _brush = null;

        private void DLSIcon_Paint(object? sender, PaintEventArgs e)
        {
            if (_brush == null)
                _brush = new SolidBrush(ForeColor);

            // Retrieve the graphics object.
            Graphics formGraphics = e.Graphics;

            // Declare a new font.
            Font myFont = new Font(_font, IconSize);

            // Change the TextRenderingHint property.
            formGraphics.TextRenderingHint =
                TextRenderingHint.AntiAliasGridFit;

            // Set the text contrast to a low-contrast setting.
            formGraphics.TextContrast = 0;

            // Draw the string again.
            formGraphics.DrawString(Char.ToString((char)Icon), myFont, _brush, 0, 0);
        }


        bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _brush?.Dispose();
                }
                _disposed = true;
            }
        }

        public enum eDLSIcon
        {
            ICO_UNKNOWN = 0,
            ICO_2D_SEISMIC = 0xE902,
            ICO_3D_GRID = 0xE903,
            ICO_3D_SEISMIC = 0xE904,
            ICO_3D_SPACE = 0xE905,
            ICO_ACTIVITY = 0xE906,
            ICO_ADD_DATA_RANGE = 0xE907,
            ICO_ADD_NEW_JOB = 0xE908,
            ICO_ADD_RESULT_TO = 0xE909,
            ICO_ADD_TEXT = 0xE90A,
            ICO_ADD_TO_REPORT = 0xE90B,
            ICO_ADD = 0xE90C,
            ICO_ADVANCED_SEARCH = 0xE90D,
            ICO_AIRPLANE = 0xE90E,
            ICO_ANALYSIS_SEARCH = 0xE90F,
            ICO_API = 0xE910,
            ICO_APPROVED_DOCUMENT = 0xE911,
            ICO_APPROVED = 0xE912,
            ICO_ARCHIVE = 0xE913,
            ICO_ARROW_DOWN_1 = 0xE914,
            ICO_ARROW_DOWN_2 = 0xE915,
            ICO_ARROW_DOWN_3 = 0xE916,
            ICO_ARROW_DOWN_4 = 0xE917,
            ICO_ARROW_DOWN_5 = 0xE918,
            ICO_ARROW_LEFT_1 = 0xE919,
            ICO_ARROW_LEFT_2 = 0xE91A,
            ICO_ARROW_LEFT_3 = 0xE91B,
            ICO_ARROW_LEFT_4 = 0xE91C,
            ICO_ARROW_LEFT_5 = 0xE91D,
            ICO_ARROW_RIGHT_1 = 0xE91E,
            ICO_ARROW_RIGHT_2 = 0xE91F,
            ICO_ARROW_RIGHT_3 = 0xE920,
            ICO_ARROW_RIGHT_4 = 0xE921,
            ICO_ARROW_RIGHT_5 = 0xE922,
            ICO_ARROW_UP_1 = 0xE923,
            ICO_ARROW_UP_2 = 0xE924,
            ICO_ARROW_UP_3 = 0xE925,
            ICO_ARROW_UP_4 = 0xE926,
            ICO_ARROW_UP_5 = 0xE927,
            ICO_ATTACHMENT = 0xE928,
            ICO_AUTO_FIT_CANVAS_C = 0xE929,
            ICO_AUTOSCAN = 0xE92A,
            ICO_AVAILABILITY = 0xE92B,
            ICO_BACKUP = 0xE92C,
            ICO_BADGE = 0xE92D,
            ICO_BALANCE = 0xE92E,
            ICO_BAR_CHART = 0xE92F,
            ICO_BASIN = 0xE930,
            ICO_BASKET = 0xE931,
            ICO_BELL_B1 = 0xE932,
            ICO_BELL_B2 = 0xE933,
            ICO_BELL_NOTIFICATION = 0xE934,
            ICO_BHA_STROKE = 0xE935,
            ICO_BIT = 0xE936,
            ICO_BLOCK_DIAGRAM = 0xE937,
            ICO_BLOCK = 0xE938,
            ICO_BOLD = 0xE939,
            ICO_BOOKMARK = 0xE93A,
            ICO_BRAIN = 0xE93B,
            ICO_BROWNFIELD = 0xE93C,
            ICO_BROWSE = 0xE93D,
            ICO_BUG = 0xE93E,
            ICO_CALCULATE_AREA = 0xE93F,
            ICO_CALCULATOR = 0xE940,
            ICO_CALENDAR = 0xE941,
            ICO_CALIPER_IN = 0xE942,
            ICO_CALIPER_OUT = 0xE943,
            ICO_CALL = 0xE944,
            ICO_CASING = 0xE945,
            ICO_CHAT = 0xE946,
            ICO_CHECK = 0xE947,
            ICO_CHECKLIST = 0xE948,
            ICO_CLEAR = 0xE949,
            ICO_CLOSE_JOB = 0xE94A,
            ICO_CLOSE = 0xE94B,
            ICO_CLOUD = 0xE94C,
            ICO_CO_RENDERING_MODE = 0xE94D,
            ICO_COALBED_METHANE = 0xE94E,
            ICO_COLLAPSE = 0xE94F,
            ICO_COLOR_PICKER = 0xE950,
            ICO_COMMENTS = 0xE951,
            ICO_COMPANY = 0xE952,
            ICO_COMPASS = 0xE953,
            ICO_COMPLETION = 0xE954,
            ICO_CONDENSATE = 0xE955,
            ICO_CONTACTS = 0xE956,
            ICO_CONTROLS = 0xE957,
            ICO_CORNER_GRIP = 0xE958,
            ICO_CROSSLINE = 0xE959,
            ICO_DASHBOARD_1 = 0xE95A,
            ICO_DASHBOARD_2 = 0xE95B,
            ICO_DATA_MARKER = 0xE95C,
            ICO_DATA = 0xE95D,
            ICO_DATABASE = 0xE95E,
            ICO_DAY_OFF_RANGE = 0xE95F,
            ICO_DAY_OFF = 0xE960,
            ICO_DEEP_WATER = 0xE961,
            ICO_DELETE = 0xE962,
            ICO_DEPTH = 0xE963,
            ICO_DESKTOP = 0xE964,
            ICO_DISCOUNT = 0xE965,
            ICO_DOC = 0xE966,
            ICO_DOCK_NAVIGATION_COLLAPSE = 0xE967,
            ICO_DOCK_NAVIGATION_EXPAND = 0xE968,
            ICO_DOCUMENT = 0xE969,
            ICO_DOCUMENTS = 0xE96A,
            ICO_DONUT_CHART = 0xE96B,
            ICO_DOWNLOAD = 0xE96C,
            ICO_DRAG = 0xE96D,
            ICO_DRY_HOLE = 0xE96E,
            ICO_DUPLICATE = 0xE96F,
            ICO_DURATION = 0xE970,
            ICO_EARTH_2 = 0xE971,
            ICO_EARTH = 0xE972,
            ICO_EDIT_1 = 0xE973,
            ICO_EDIT_2 = 0xE974,
            ICO_EDIT_USER = 0xE975,
            ICO_EDIT = 0xE976,
            ICO_EMAIL = 0xE977,
            ICO_EMOJI = 0xE978,
            ICO_ENGG = 0xE979,
            ICO_EQUIPMENT = 0xE97A,
            ICO_ERROR = 0xE97B,
            ICO_EXCHANGE = 0xE97C,
            ICO_EXPAND_1 = 0xE97D,
            ICO_EXPAND_2 = 0xE97E,
            ICO_EXPORT = 0xE97F,
            ICO_FACILITIES = 0xE980,
            ICO_FEEDBACK = 0xE981,
            ICO_FIELD = 0xE982,
            ICO_FILTER = 0xE983,
            ICO_FLAG = 0xE984,
            ICO_FLOW_DESIGNER = 0xE985,
            ICO_FLUID = 0xE986,
            ICO_FRACTURE = 0xE987,
            ICO_FULLSCREEN = 0xE988,
            ICO_GANTT_CHART_VIEW = 0xE989,
            ICO_GAS = 0xE98A,
            ICO_GEOMETRY = 0xE98B,
            ICO_GLOBAL_NAVIGATION = 0xE98C,
            ICO_GRAPH = 0xE98D,
            ICO_GREENFIELD = 0xE98E,
            ICO_GRID_VIEW = 0xE98F,
            ICO_HAMBURGER = 0xE990,
            ICO_HAND = 0xE991,
            ICO_HASHTAG = 0xE992,
            ICO_HEADSET = 0xE993,
            ICO_HEATMAP = 0xE994,
            ICO_HELP = 0xE995,
            ICO_HIERARCHY = 0xE996,
            ICO_HOME = 0xE997,
            ICO_HORIZON = 0xE998,
            ICO_IMAGE = 0xE999,
            ICO_IMPORT = 0xE99A,
            ICO_INBOUND_SHIPMENT = 0xE99B,
            ICO_INBOX = 0xE99C,
            ICO_INFO = 0xE99D,
            ICO_INJECTION = 0xE99E,
            ICO_INLINE_CROSSLINE_DEPTH = 0xE99F,
            ICO_INLINE_CROSSLINE = 0xE9A0,
            ICO_INLINE = 0xE9A1,
            ICO_INVENTORY = 0xE9A2,
            ICO_ITALIC = 0xE9A3,
            ICO_JOB_HEADER = 0xE9A4,
            ICO_JOB_MANAGEMENT = 0xE9A5,
            ICO_KEYBOARD_SHORTCUT = 0xE9A6,
            ICO_LAPTOP = 0xE9A7,
            ICO_LAUNCH = 0xE9A8,
            ICO_LAYERS = 0xE9A9,
            ICO_LESSON_LEARNED = 0xE9AA,
            ICO_LIKE = 0xE9AB,
            ICO_LIKE_SOLID = 0xE9AC,
            ICO_LINE_CHART = 0xE9AD,
            ICO_LINK = 0xE9AE,
            ICO_LIST_VIEW = 0xE9AF,
            ICO_LIST = 0xE9B0,
            ICO_LITHOLOGY = 0xE9B1,
            ICO_LOCATION = 0xE9B2,
            ICO_LOCKED = 0xE9B3,
            ICO_LOG = 0xE9B4,
            ICO_LOGIN = 0xE9B5,
            ICO_LOGISTICS = 0xE9B6,
            ICO_LOGOUT_1 = 0xE9B7,
            ICO_LOGOUT_2 = 0xE9B8,
            ICO_MAINTENANCE = 0xE9B9,
            ICO_MANAGEMENT = 0xE9BA,
            ICO_MAP_PIN = 0xE9BB,
            ICO_MAP = 0xE9BC,
            ICO_MARKER = 0xE9BD,
            ICO_MEASURE_DISTANCE = 0xE9BE,
            ICO_MEASUREMENTS = 0xE9BF,
            ICO_MENTION = 0xE9C0,
            ICO_MINUS = 0xE9C1,
            ICO_MOBILE = 0xE9C2,
            ICO_MONITORING = 0xE9C3,
            ICO_MOON = 0xE9C4,
            ICO_MORE = 0xE9C5,
            ICO_MOVE = 0xE9C6,
            ICO_MUD_TEST = 0xE9C7,
            ICO_NETWORK = 0xE9C8,
            ICO_NEW_DOC = 0xE9C9,
            ICO_NEW_SCREEN = 0xE9CA,
            ICO_NEWSPAPER = 0xE9CB,
            ICO_NO_FLUID = 0xE9CC,
            ICO_NO_PREVIEW = 0xE9CD,
            ICO_NO_SIGNAL = 0xE9CE,
            ICO_NO_SOUND = 0xE9CF,
            ICO_NOTES = 0xE9D0,
            ICO_NOTIFICATION = 0xE9D1,
            ICO_NUMBERED_LIST = 0xE9D2,
            ICO_OFFLINE = 0xE9D3,
            ICO_OFFSHORE = 0xE9D4,
            ICO_OIL_AND_GAS = 0xE9D5,
            ICO_OIL_BITUMEN = 0xE9D6,
            ICO_OIL = 0xE9D7,
            ICO_ONLINE_KNOWLEDGE = 0xE9D8,
            ICO_ONLINE = 0xE9D9,
            ICO_ONSHORE = 0xE9DA,
            ICO_OPERATOR = 0xE9DB,
            ICO_ORDER = 0xE9DC,
            ICO_OUTBOUND_SHIPMENT = 0xE9DD,
            ICO_PASS = 0xE9DE,
            ICO_PAYROLL = 0xE9DF,
            ICO_PDF = 0xE9E0,
            ICO_PERFORATION = 0xE9E1,
            ICO_PERSONNEL = 0xE9E2,
            ICO_PIE_CHART = 0xE9E3,
            ICO_PIPELINE = 0xE9E4,
            ICO_PLAN_DOCUMENT = 0xE9E5,
            ICO_PLATFORM = 0xE9E6,
            ICO_PLAY = 0xE9E7,
            ICO_PLUGGED_AND_ABANDONED = 0xE9E8,
            ICO_PLUS = 0xE9E9,
            ICO_POINTS = 0xE9EA,
            ICO_POLAR_CHART = 0xE9EB,
            ICO_POLAR_POINT = 0xE9EC,
            ICO_PPT = 0xE9ED,
            ICO_PRESSURE = 0xE9EE,
            ICO_PREVIEW = 0xE9EF,
            ICO_PRINT = 0xE9F0,
            ICO_PROFILE_INITIALS = 0xE9F1,
            ICO_PROFILE = 0xE9F2,
            ICO_PTX = 0xE9F3,
            ICO_QHSE_SAFETY = 0xE9F4,
            ICO_QUOTE = 0xE9F5,
            ICO_REASSIGN_USER = 0xE9F6,
            ICO_REDO = 0xE9F7,
            ICO_REDUCE_SCREEN = 0xE9F8,
            ICO_REFRESH = 0xE9F9,
            ICO_REJECT_DOCUMENT = 0xE9FA,
            ICO_REMOVE_TAG = 0xE9FB,
            ICO_REMOVE = 0xE9FC,
            ICO_REPLY = 0xE9FD,
            ICO_REPORT = 0xE9FE,
            ICO_RESERVOIR = 0xE9FF,
            ICO_RESET = 0xEA00,
            ICO_RIG = 0xEA01,
            ICO_ROTATE_LEFT = 0xEA02,
            ICO_ROTATE_RIGHT = 0xEA03,
            ICO_SAVE_AS = 0xEA04,
            ICO_SAVE = 0xEA05,
            ICO_SCALE = 0xEA06,
            ICO_SCHEDULER = 0xEA07,
            ICO_SEARCH = 0xEA08,
            ICO_SEISMIC_DRIVE = 0xEA09,
            ICO_SEISMIC_GENERIC = 0xEA0A,
            ICO_SELECT = 0xEA0B,
            ICO_SENSOR_MANAGEMENT = 0xEA0C,
            ICO_SENSOR = 0xEA0D,
            ICO_SETTINGS_SMALL = 0xEA0E,
            ICO_SETTINGS = 0xEA0F,
            ICO_SHALLOW_HOLE_TEST = 0xEA10,
            ICO_SHALLOW_WATER = 0xEA11,
            ICO_SHARE = 0xEA12,
            ICO_SHIP_FORWARD = 0xEA13,
            ICO_SHIP_SIDE = 0xEA14,
            ICO_SIGNAL = 0xEA15,
            ICO_SIGNATURE = 0xEA16,
            ICO_SIMULATION = 0xEA17,
            ICO_SNAPSHOT = 0xEA18,
            ICO_SORT = 0xEA19,
            ICO_SOUND = 0xEA1A,
            ICO_STAGE = 0xEA1B,
            ICO_STAR = 0xEA1C,
            ICO_STOP = 0xEA1D,
            ICO_STRETCH_SQUEEZE = 0xEA1E,
            ICO_SUN = 0xEA1F,
            ICO_SURFACE_LOCATION = 0xEA20,
            ICO_SURFACE = 0xEA21,
            ICO_SURVEY = 0xEA22,
            ICO_SYNC = 0xEA23,
            ICO_TABLE_VIEW = 0xEA24,
            ICO_TABLET = 0xEA25,
            ICO_TAG = 0xEA26,
            ICO_TARGET = 0xEA27,
            ICO_TASKS = 0xEA28,
            ICO_TEAM = 0xEA29,
            ICO_TEMPERATURE = 0xEA2A,
            ICO_TEST = 0xEA2B,
            ICO_TEXT = 0xEA2C,
            ICO_THUMBS_DOWN = 0xEA2D,
            ICO_THUMBS_UP = 0xEA2E,
            ICO_TIME = 0xEA2F,
            ICO_TOOLKIT = 0xEA30,
            ICO_TOOLS = 0xEA31,
            ICO_TRAJECTORY = 0xEA32,
            ICO_TREATMENT = 0xEA33,
            ICO_TUBING = 0xEA34,
            ICO_TXT = 0xEA35,
            ICO_UNDERLINE = 0xEA36,
            ICO_UNDO = 0xEA37,
            ICO_UNLOCKED = 0xEA38,
            ICO_UPLOAD = 0xEA39,
            ICO_USER_SALES = 0xEA3A,
            ICO_USER = 0xEA3B,
            ICO_VIEW_FILE = 0xEA3C,
            ICO_VIEWER = 0xEA3D,
            ICO_VOICE_CONTROL = 0xEA3E,
            ICO_WARNING = 0xEA3F,
            ICO_WATER = 0xEA40,
            ICO_WAVE_PLOT = 0xEA41,
            ICO_WELL = 0xEA42,
            ICO_WELLHEAD = 0xEA43,
            ICO_XLS = 0xEA44,
            ICO_ZOOM_IN = 0xEA45,
            ICO_ZOOM_OUT = 0xEA46
        }
    }
}
