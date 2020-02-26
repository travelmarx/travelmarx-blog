"""SomeClass.py - docstring before class definition."""

from datetime import datetime

import functools
import time

def timer(func):
    """Print the runtime of the decorated function"""
    @functools.wraps(func)
    def wrapper_timer(*args, **kwargs):
        start_time = time.perf_counter()    # 1
        value = func(*args, **kwargs)
        end_time = time.perf_counter()      # 2
        run_time = end_time - start_time    # 3
        print(f"Finished {func.__name__!r} in {run_time:.4f} secs")
        return value
    return wrapper_timer

class SomeClass(object):
    """Creates SomeClass instance in package1.

    .. remarks::

        This is the remarks field for SomeClass. You can have inline code ``SomeClass.method1()``
        or you can include code blocks. Here's a link to an external site
        `Bing <http://www.bing.com>`_.

        Here's a link to a method in this class: :meth:`method1`. Here's a link to a class in
        a different package: :class:`core_api.package2.anotherclass.AnotherClass`.
        currently.

        Note that whitespace at end of lines are not checked in this setup. There are not policy
        gates or PEP checks enforced.

        .. note::

            This is a note. This is my first test class so it doesn't have much doc
            to show.

        .. note::
            This is a note. This is my first test class so it doesn't have much doc
            to show. But how does wrapping look?

            I guess in normal markup you would just put one long line and not worry about the docstring limit which is the real problem here, note that the sphinx processing that is doing the right thing.

    :var SomeClass.classAttribute1: Some class attribute 1. Does ```it``` respect ``code`` markup?
        Or any **other** type? The answer seems to be yes. Note that qualifying the attribute
        means the type doesn't show up.
    :vartype classAttribute1: str
    :var classAttribute2: Some class attribute 2.
    :vartype classAttribute2: bool

    :param arg1: Description of arg1.
    :type arg1: str
    :param arg2: Description of arg2.
    :type arg2: int
    """

    #: This is class property myvar.
    #: It defined a simple variable with `code`.
    myvar = 1

    def __init__(self, arg1):
        """Initialize SomeClass in package1.

        :param arg1: Description of arg1.
        :type arg1: str
        """

    def method1(self, arg1=True):
        """Test table formats of method1.

        :param arg1: Description of arg1. The following table describes
            this parameter.

            ======              =============================================
            Value                   Behavior
            ======              =============================================
            true                Default. Dataset is visible in workspace UI.
                                Wrapped looks like what?

            false               Dataset is hidden
            ======              =============================================

            +----------+---------------------------------------+
            |  Value   |              Behavior                 |
            +----------+---------------------------------------+
            |   true   |  Default. Dataset is visible in       |
            |          |  Workspace UI.                        |
            +----------+---------------------------------------+
            |  false   |  Dataset is hidden in Workspace UI.   |
            +----------+---------------------------------------+

            Let's make a nested list:

            * Item 1

                * abc
                * zyy

            * Item 2

        :type arg1: bool
        :return: A message (rtype doesn't seem to be supported).
        """

    def method2(self, arg1, arg2):
        """Docstring of method2.

        :param arg1: Describing the first parameter of method2().
        :param arg2: Describing the second parameter of method2().

        .. remarks::

            The remarks block may be before the parameter
            definitions in .py file but it always appears after them.

            .. code-block:: python
                :emphasize-lines: 19

                from azureml.core import Workspace, Experiment
                ws = Workspace.from_config()

                from os import path, makedirs
                experiment_name = 'tensorboard-demo'

                # experiment folder
                exp_dir = './sample_projects/' + experiment_name

                if not path.exists(exp_dir):
                    makedirs(exp_dir)

                # runs we started in this session, for the finale
                runs = []

                # Generate Tensorflow log files by running Tensorflow locally,
                # training in a remote VM, remote AmlCompute cluster, etc.

                from azureml.tensorboard import Tensorboard

                # The Tensorboard constructor takes an array of runs, so be sure and pass it in as a single-element array here
                tb = Tensorboard([run])

                # If successful, start() returns a string with the URI of the instance.
                tb.start()

                # When done, stop
                tb.stop()
        """
    pass

    @timer
    def waste_some_time(a):
        """waste time 1"""
    pass

    @timer
    def waste_some_time(a, b):
        """waste time 2"""
    pass
