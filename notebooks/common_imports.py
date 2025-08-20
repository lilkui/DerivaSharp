from pythonnet import load

load("coreclr")

import os
import clr

clr.AddReference(os.path.abspath("../src/DerivaSharp/bin/Release/net9.0/win-x64/publish/DerivaSharp.dll"))

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd

import DerivaSharp.Instruments as Instruments
import DerivaSharp.PricingEngines as PricingEngines
from System import DateOnly
