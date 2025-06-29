import sys
import cv2
from ultralytics import YOLO
import easyocr

def detect_and_ocr(image_path, model_path):
    # Load the YOLO model
    model = YOLO(model_path)  # Use the provided model path

    # Run inference
    results = model(image_path)

    # Read image
    img = cv2.imread(image_path)
    detected_text = ""

    for r in results:
        boxes = r.boxes
        for box in boxes:
            x1, y1, x2, y2 = map(int, box.xyxy[0])  # Bounding box coordinates
            conf = box.conf.item()  # Confidence score

            if conf > 0.5:  # Only process confident detections
                cropped_img = img[y1:y2, x1:x2]

                # OCR with EasyOCR
                reader = easyocr.Reader(['en'])
                ocr_result = reader.readtext(cropped_img)

                # Combine recognized text
                if ocr_result:
                    detected_text = " ".join([res[1] for res in ocr_result])

    print(detected_text.strip())

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Error: Image path and model path required.")
        sys.exit(1)

    image_path = sys.argv[1]
    model_path = sys.argv[2]
    detect_and_ocr(image_path, model_path)