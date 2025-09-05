# Automated Image Resizer 🖼️

A simple, serverless application built on Azure that automatically creates thumbnails when you upload an image to Blob Storage. This project is a basic introduction to event-driven architecture using Azure Event Grid.

## **Core Technologies**

* **Azure Blob Storage:** Stores the original and thumbnail images.
* **Azure Event Grid:** Routes creation events from Storage to the Function.
* **Azure Functions:** Runs the C# code to resize the image.

***

## **How It Works**

1.  An image is uploaded to the `uploads` container in Blob Storage.
2.  Azure Event Grid detects this "Blob Created" event.
3.  An Event Grid subscription, filtered to watch only the `uploads` container, forwards the event.
4.  An Azure Function is triggered by the event.
5.  The function downloads the image, resizes it, and saves the new thumbnail to the `thumbnails` container.

***

## **Setup Guide**

1.  **Create Resources:** In Azure, create a Storage Account with two containers (`uploads` and `thumbnails`) and a .NET-based Azure Function App.
2.  **Deploy Code:** Publish the C# function code to your Function App.
3.  **Connect with Event Grid:** In your Storage Account's **Events** section, create a new **Event Subscription**.
    * **Endpoint:** Point it to your Azure Function.
    * **Filter:** Set a **Subject Filter** to `/blobServices/default/containers/uploads/` to prevent infinite loops.

***

## **How to Test**

Simply upload a `.jpg` or `.png` file to the `uploads` container and check the `thumbnails` container a few seconds later for the resized image.
