"""SomeClass.py - docstring before class definition."""

from datetime import datetime

class SomeClass(object):
    """Docstring description for SomeClass class in package1.

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

    :param arg1: Description of arg1.
    :type arg1: str
    :param arg2: Description of arg2.
    :type arg2: int
    """

    def __init__(self, arg1, arg2):
        """Initialize SomeClass in package1.

        :param arg1: Description of arg1.
        :type arg1: str
        :param arg2: Description of arg2.
        :type arg2: int
        """
        pass

    def method1(self, arg1=None):
        """Docstring description of method1.

        :param arg1: Description of arg1. The following table describes
            this parameter.

            +----------+---------------------------------------+
            |  Value   |              Behavior                 |
            +----------+---------------------------------------+
            |   True   |  Default. Dataset is visible in       |
            |          |  Workspace UI.                        |
            +----------+---------------------------------------+
            |  False   |  Dataset is hidden in Workspace UI.   |
            +----------+---------------------------------------+

        :type arg1: bool
        :return: Description of return value.
        """
        pass

    def method2(self, arg1, arg2):
        """Docstring of method2.

        :param arg1: Describing the first parameter of method2().
        :type arg1: dict
        :param arg2: Describing the second parameter of method2().
        :type arg2: bool

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