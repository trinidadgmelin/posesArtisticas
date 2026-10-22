"""
pose_to_unity.py  (v4 - esqueleto completo, MediaPipe Tasks API 0.10.x / 1.x)
Webcam / DroidCam -> MediaPipe PoseLandmarker -> UDP (127.0.0.1:7000) -> Unity

Mensaje (texto plano, coma como separador, punto decimal) = 13 puntos, 26 valores X,Y:
    [0-9]   nariz, muñeca izq, muñeca der, tobillo izq, tobillo der   (igual que antes)
    [10-25] hombro izq, hombro der, codo izq, codo der,
            cadera izq, cadera der, rodilla izq, rodilla der
Un receptor viejo que lee solo los primeros 10 valores sigue funcionando.
Coordenadas normalizadas [0..1], origen arriba-izquierda (Y crece hacia abajo),
imagen SIN espejar. Salir: tecla 'q' con la ventana enfocada.

La primera vez descarga el modelo (~9 MB) al lado del script.
"""

import platform
import socket
import sys
import time
import traceback
import urllib.request
from pathlib import Path

# ======================= CONFIGURACIÓN =======================
# Opciones para CAMERA_SOURCE:
#   None                              -> prueba índices 0..5 y usa el primero que entregue imagen
#   1                                 -> índice fijo (la cámara virtual de DroidCam suele ser 1 o 2)
#   "http://192.168.0.23:4747/video"  -> stream directo de DroidCam por WiFi (IP que muestra la app)
CAMERA_SOURCE = None

UDP_IP = "127.0.0.1"
UDP_PORT = 7000
MIRROR_PREVIEW = True        # solo afecta la ventana, NO los datos enviados
MAX_FAILED_READS = 60        # frames fallidos seguidos antes de abortar

MODEL_URL = ("https://storage.googleapis.com/mediapipe-models/pose_landmarker/"
             "pose_landmarker_full/float16/latest/pose_landmarker_full.task")
MODEL_PATH = Path(__file__).resolve().with_name("pose_landmarker_full.task")
# =============================================================

# El ORDEN importa: Unity lo lee en este orden. Los primeros 5 son los de colisión.
LANDMARK_IDS = (0, 15, 16, 27, 28,          # nariz, muñecas, tobillos
                11, 12, 13, 14,             # hombros, codos
                23, 24, 25, 26)             # caderas, rodillas
COLLISION_IDS = {0, 15, 16, 27, 28}
# Huesos para dibujar el monigote en la preview
BONES = [(11, 12), (11, 13), (13, 15), (12, 14), (14, 16),
         (11, 23), (12, 24), (23, 24),
         (23, 25), (25, 27), (24, 26), (26, 28)]
IS_WINDOWS = platform.system() == "Windows"


def log(msg):
    print(msg, flush=True)


# ------------------------- Modelo -------------------------
def ensure_model():
    if MODEL_PATH.exists() and MODEL_PATH.stat().st_size > 1_000_000:
        return
    log(f"[model] Descargando modelo de pose a {MODEL_PATH} ...")
    try:
        tmp = MODEL_PATH.with_suffix(".part")
        urllib.request.urlretrieve(MODEL_URL, tmp)
        tmp.replace(MODEL_PATH)
        log(f"[model] OK ({MODEL_PATH.stat().st_size // 1024} KB)")
    except Exception as e:
        raise RuntimeError(
            f"No se pudo descargar el modelo: {e}\n"
            f"  Bajalo a mano desde el navegador:\n  {MODEL_URL}\n"
            f"  y guardalo como:\n  {MODEL_PATH}"
        )


# ------------------------- Cámara -------------------------
def try_open(source):
    """Prueba la fuente con varios backends y devuelve el primero que entregue frames."""
    import cv2
    if isinstance(source, int) and IS_WINDOWS:
        backends = [("DSHOW", cv2.CAP_DSHOW), ("MSMF", cv2.CAP_MSMF), ("ANY", cv2.CAP_ANY)]
    elif isinstance(source, str):
        backends = [("FFMPEG", cv2.CAP_FFMPEG), ("ANY", cv2.CAP_ANY)]
    else:
        backends = [("ANY", cv2.CAP_ANY)]

    for name, api in backends:
        cap = cv2.VideoCapture(source, api)
        if not cap.isOpened():
            log(f"[cam]     {source!r} con {name}: no abre")
            cap.release()
            continue
        for _ in range(40):  # las cámaras virtuales suelen fallar los primeros frames
            ok, frame = cap.read()
            if ok and frame is not None and frame.size > 0:
                h, w = frame.shape[:2]
                log(f"[cam]     {source!r} con {name}: OK ({w}x{h})")
                return cap
            time.sleep(0.05)
        log(f"[cam]     {source!r} con {name}: abre pero no entrega imagen")
        cap.release()
    return None


def open_camera():
    global CAMERA_SOURCE
    # Permite: python pose_to_unity.py 1   o   python pose_to_unity.py http://IP:4747/video
    if len(sys.argv) > 1:
        arg = sys.argv[1]
        CAMERA_SOURCE = int(arg) if arg.isdigit() else arg

    if CAMERA_SOURCE is not None:
        log(f"[cam] Abriendo fuente configurada: {CAMERA_SOURCE!r}")
        cap = try_open(CAMERA_SOURCE)
        if cap is None:
            raise RuntimeError(
                f"No se pudo leer imagen de {CAMERA_SOURCE!r}.\n"
                "  - Si es índice: probá otro número o dejá CAMERA_SOURCE = None.\n"
                "  - Si es URL: chequeá la IP en la app, misma WiFi en PC y celu,\n"
                "    y el cliente DroidCam de la PC DESCONECTADO (acepta un solo cliente)."
            )
        return cap

    log("[cam] Buscando cámaras en índices 0..5 ...")
    for idx in range(6):
        cap = try_open(idx)
        if cap is not None:
            log(f"[cam] Usando índice {idx}. Si no es DroidCam, fijá CAMERA_SOURCE a otro número.")
            return cap
        log(f"[cam]   índice {idx}: sin imagen")
    raise RuntimeError(
        "Ninguna cámara entregó imagen.\n"
        "  - Abrí el cliente DroidCam en la PC y dale 'Start' ANTES de correr el script.\n"
        "  - O usá el stream directo: CAMERA_SOURCE = \"http://IP_DEL_CELU:4747/video\""
    )


# ------------------------- Main -------------------------
def main():
    log("[init] Importando OpenCV y MediaPipe (tarda unos segundos)...")
    import cv2
    import mediapipe as mp
    from mediapipe.tasks.python import vision
    from mediapipe.tasks.python.core.base_options import BaseOptions
    log(f"[init] OK  mediapipe {mp.__version__} | opencv {cv2.__version__} | python {platform.python_version()}")

    ensure_model()

    # Se carga como buffer para evitar problemas con rutas en Windows
    options = vision.PoseLandmarkerOptions(
        base_options=BaseOptions(model_asset_buffer=MODEL_PATH.read_bytes()),
        running_mode=vision.RunningMode.VIDEO,
        num_poses=1,
        min_pose_detection_confidence=0.5,
        min_pose_presence_confidence=0.5,
        min_tracking_confidence=0.5,
    )

    cap = open_camera()
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    target = (UDP_IP, UDP_PORT)
    window = "Pose -> Unity  |  'q' para salir"
    cv2.namedWindow(window, cv2.WINDOW_NORMAL)
    log(f"[run] Enviando a {UDP_IP}:{UDP_PORT}. Apretá 'q' en la ventana para salir.")

    failed = 0
    sent = 0
    t0 = time.monotonic()
    last_ts = -1

    with vision.PoseLandmarker.create_from_options(options) as landmarker:
        while True:
            ok, frame = cap.read()
            if not ok or frame is None:
                failed += 1
                if failed >= MAX_FAILED_READS:
                    log("[run] La cámara dejó de entregar frames. Cortando.")
                    break
                cv2.waitKey(10)
                continue
            failed = 0

            # Modo VIDEO exige timestamps en ms estrictamente crecientes
            ts = int((time.monotonic() - t0) * 1000)
            if ts <= last_ts:
                ts = last_ts + 1
            last_ts = ts

            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
            result = landmarker.detect_for_video(mp_image, ts)

            preview = cv2.flip(frame, 1) if MIRROR_PREVIEW else frame.copy()
            h, w = preview.shape[:2]

            if result.pose_landmarks:
                lm = result.pose_landmarks[0]  # primera (y única) persona
                values = []
                for i in LANDMARK_IDS:
                    values.append(f"{lm[i].x:.4f}")
                    values.append(f"{lm[i].y:.4f}")

                # --- Preview: monigote (blanco) + puntos de colisión (verde) ---
                def px(i):
                    dx = (1.0 - lm[i].x) if MIRROR_PREVIEW else lm[i].x
                    return int(dx * w), int(lm[i].y * h)

                for a, b in BONES:
                    cv2.line(preview, px(a), px(b), (255, 255, 255), 3, cv2.LINE_AA)
                (s1x, s1y), (s2x, s2y) = px(11), px(12)
                cv2.line(preview, ((s1x + s2x) // 2, (s1y + s2y) // 2), px(0),
                         (255, 255, 255), 3, cv2.LINE_AA)            # cuello
                for i in LANDMARK_IDS:
                    if i in COLLISION_IDS:
                        cv2.circle(preview, px(i), 10, (0, 255, 0), -1)
                    else:
                        cv2.circle(preview, px(i), 5, (255, 200, 0), -1)

                message = ",".join(values)
                sock.sendto(message.encode("utf-8"), target)
                sent += 1
                if sent == 1:
                    log(f"[udp] Primer mensaje: {message}")
                status, color = f"UDP -> {UDP_IP}:{UDP_PORT}  ({sent})", (0, 255, 0)
            else:
                status, color = "Sin pose detectada (no se envia)", (0, 0, 255)

            cv2.putText(preview, status, (10, 25),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2, cv2.LINE_AA)
            cv2.imshow(window, preview)

            if cv2.waitKey(1) & 0xFF == ord("q"):
                break
            if cv2.getWindowProperty(window, cv2.WND_PROP_VISIBLE) < 1:  # cerrar con la X
                break

    cap.release()
    cv2.destroyAllWindows()
    sock.close()
    log("[end] Listo.")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        log("\n========== ERROR ==========")
        traceback.print_exc()
        log("===========================")
        input("\nApretá Enter para cerrar...")
        sys.exit(1)
