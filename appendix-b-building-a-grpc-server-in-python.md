---
title: "Appendix B: Creating a gRPC Server in Python"
layout: module
nav_order: 102
summary: >
    Building a gRPC server using Python and grpcio-tools that can communicate with the .NET PricingClient.
---

One of the very cool things about gRPC is that it has excellent cross-platform support, so you can use different languages and platforms to create the different components of your distributed application.

Here's how to build a simple gRPC pricing server using Python that will communicate with the `Autobarn.PricingClient` we created during the workshop.

> You'll need Python for these examples. To check whether you've got it installed, type 
>
> `python --version`
>
> at a console prompt. If you get `Python 3.10.0` (or anything starting with Python 3), you're good to go. If not, you'll need to install Python first.

Create a `python` directory in the `Autobarn` project directory:

```bash
D:\autobarn> mkdir python
D:\autobarn> cd python
```

Install the `grpcio-tools`:

```bash
D:\autobarn\python> python -m pip install grpcio-tools
```

Copy the `price.proto` file from the `Autobarn.PricingServer` project.

Run the Python `grpc_tools.protoc` compiler:

```bash
python -m grpc_tools.protoc -I . --python_out=. --grpc_python_out=. --proto_path . price.proto
```

* `python -m grpc_tools.protoc` runs the protocol buffers compiler that's bundled with `grpcio-tools`
* `-I .` specifies the directory where any files imported by our `.proto` file can be found. (We don't have any, but we still have to specify this, because computers are stupid.)
* `--python_out=. --grpc_python_out=.` specifies the output folders where the generated Python code will go
* `--proto_path .` specifies the folder to search for `.proto` files.
* `price.proto` is the name of our protocol spec file.

Now you need to implement the pricing service, and add the grpc endpoints. You'll end up with something like this:

```python
import grpc
from concurrent import futures
import time
import os
import price_pb2_grpc as pb2_grpc
import price_pb2 as pb2

class PricingService(pb2_grpc.PricerServicer):
    def __init__(self, *args, **kwargs):
        pass

    def GetPrice(self, request, context):        
        print(request)
        if "ford" in request.modelCode.lower():
            result = {'price': 5000, 'currencyCode': 'EUR'}
        elif "brown" in request.color.lower():
            result = {'price': 1250, 'currencyCode': 'SEK'}
        elif "delorean" in request.modelCode.lower():
            result = {'price': 50000, 'currencyCode': 'USD'}
        else:
            price = 1000 + (request.year * 10)
            result = {'price': price, 'currencyCode': 'GBP '}

        return pb2.PriceReply(**result)

def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    pb2_grpc.add_PricerServicer_to_server(PricingService(), server)
    server.add_insecure_port('[::]:5002')

    # If you've created a localhost HTTPS certificate, uncomment these lines to use it.
    # key = open('localhost.key', 'rb').read()
    # crt = open('localhost.crt', 'rb').read()
    # server_credentials = grpc.ssl_server_credentials(((key, crt,),))
    # server.add_secure_port('[::]:5003', server_credentials)
    
    server.start()
    print("Autobarn gRPC Pricing Server running.")
    server.wait_for_termination()

if __name__ == '__main__':
    serve()
```

> This example can be used with an HTTPS certificate created by following the instructions in [appendix A](appendix-a-generating-self-signed-https-certificates-for-localhost).

Run your server with:

```bash
python pricing_server.py
```

(Remember you can't run two services on port 5003 at the same time, so if you're still running the .NET `Autobarn.PricingServer` it'll crash.)

