from utils import *
import pytesseract

path = 'text3.png'
hsv = [20, 40, 150, 255, 150, 255]
pytesseract.pytesseract.tesseract_cmd = 'C:\\Program Files\\Tesseract-OCR\\tesseract.exe'

### Erster Schritt
img = cv2.imread(path)
#cv2.imshow("Original", img)

### Zweiter schritt
imgResult = detectColor(img, hsv)

### Dritter & Vierter Schritt
imgContours, contours = getContours(imgResult, img, showCanny=False,
                                    minArea=1000, filter=4,
                                    cThr=[100, 150], draw=True)
cv2.imshow("imgContours", imgContours)
#print(len(contours))

### Fuenfter Schritt
roiList = getRoi(img, contours)
roiDisplay(roiList)

### Sechster Schritt
highlightedText= []
for x, roi in enumerate(roiList):
    highlightedText.append(pytesseract.image_to_string(roi))
    print(pytesseract.image_to_string(roi))
####lässt das Programm auf irgendeine Eingabe warten, damit es nicht sofort terminiert
cv2.waitKey(0)
cv2.destroyAllWindows()