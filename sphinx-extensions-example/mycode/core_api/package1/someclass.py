"""SomeClass.py - docstring before class definition."""

from datetime import datetime

import functools
import time

class SomeClass(object):
    """
    Creates SomeClass instance (this is the class description).

    Class descriptions are one sentence followed by optional paragraphs of text and formatting.
    This is a link to a :class:`str`.

    Here are links using markup to make external links easier to work with. See :wiki:`Machine_learning`,
    :wiki:`Supervised_learning`, and :wiki:`Unsupervised_learning`.

    * Some links to Python base objects

        * :class:`str`
        * :class:`bool`
        * :class:`list`

    * Links to other document sets

        * :class:`pandas.DataFrame`
        * :mod:`matplotlib.image`
        * :class:`numpy.ndarray`

    .. remarks::

        This text is under the remarks DocFx directive. If viewing in DocFx HTML, a Remarks section
        is generated. If viewing in Sphinx HTML, this text is added to the description.

    :param arg1: Description of class arg1.
    :type arg1: str
    """

    def __init__(self, arg1=None):
        """Initialize SomeClass in package1.

        :param arg1: Description of class arg1.
        :type arg1: str
        """
        pass

    def method1(self, arg1=True, arg2=None, arg3=None):
        """Test different external links.

        Note that the return type is a pandas.DataFrame and it links off to that documentation.
        The parameters of method1 also link to correct document sets.

        :param arg1: Description of arg1. The following table describes
            this parameter.
        :type arg1: bool
        :param arg2: Either a string or numpy array.
        :type arg2: str or numpy.array
        :param arg3: A matplotimage.
        :type arg3: matplotlib.image
        :rtype: pandas.DataFrame
        :return: A dataframe.
        """

    def method2(self, meth2_arg1, meth2_arg2):
        """Description of method2.

        :param meth2_arg1: Describing the first parameter of method2().
        :type meth2_arg1: float
        :param meth2_arg2: Describing the second parameter of method2().
        :type meth2_arg2: int
        """
    pass

